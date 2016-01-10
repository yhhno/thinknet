﻿using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using ThinkLib.Caching;


namespace ThinkNet.Common
{
    /// <summary>
    /// 设置或获取聚合的缓存接口
    /// </summary>
    [RequiredComponent(typeof(MemoryCache))]
    public interface IMemoryCache
    {
        /// <summary>
        /// 从缓存获取聚合实例
        /// </summary>
        object Get(Type type, object key);
        /// <summary>
        /// 设置一个聚合实例入缓存。不存在加入缓存，存在更新缓存
        /// </summary>
        void Set(object entity, object key);
        /// <summary>
        /// 从缓存中移除聚合根
        /// </summary>
        void Remove(Type type, object key);
    }
    internal class MemoryCache : IMemoryCache
    {
        internal readonly static IMemoryCache Instance = new MemoryCache();

        private readonly BinaryFormatter _serializer;
        private readonly bool _enabled;
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public MemoryCache()
        {
            this._serializer = new BinaryFormatter();
            this._enabled = ConfigurationManager.AppSettings["thinkcfg.caching_enabled"].Safe("false").ToBoolean();
        }

        private byte[] Serialize(object obj)
        {
            using (var stream = new MemoryStream()) {
                _serializer.Serialize(stream, obj);
                return stream.ToArray();
            }
        }

        private object Deserialize(byte[] data)
        {
            using (var stream = new MemoryStream(data)) {
                return _serializer.Deserialize(stream);
            }
        }

        /// <summary>
        /// 从缓存中获取该类型的实例。
        /// </summary>
        public object Get(Type type, object key)
        {
            if (!_enabled)
                return null;

            Ensure.NotNull(type, "type");
            Ensure.NotNull(key, "key");


            string cacheRegion = GetCacheRegion(type);
            string cacheKey = BuildCacheKey(type, key);

            object data = null;
            lock (cacheKey) {
                data = CacheManager.GetCache(cacheRegion).Get(cacheKey);
            }
            if (data == null)
                return null;

            return this.Deserialize((byte[])data);
        }
        /// <summary>
        /// 设置实例到缓存
        /// </summary>
        public void Set(object entity, object key)
        {
            if (!_enabled)
                return;

            Ensure.NotNull(entity, "entity");
            Ensure.NotNull(key, "key");

            var type = entity.GetType();

            string cacheRegion = GetCacheRegion(type);
            string cacheKey = BuildCacheKey(type, key);

            var data = this.Serialize(entity);

            lock (cacheKey) {
                CacheManager.GetCache(cacheRegion).Put(cacheKey, data);
            }
        }
        /// <summary>
        /// 从缓存中移除
        /// </summary>
        public void Remove(Type type, object key)
        {
            if (!_enabled)
                return;

            Ensure.NotNull(type, "type");
            Ensure.NotNull(key, "key");

            string cacheRegion = GetCacheRegion(type);
            string cacheKey = BuildCacheKey(type, key);

            lock (cacheKey) {
                CacheManager.GetCache(cacheRegion).Remove(cacheKey);
            }
        }


        private static string GetCacheRegion(Type type)
        {
            var attr = type.GetAttribute<CacheRegionAttribute>(false);
            if (attr == null)
                return CacheManager.CacheRegion;

            return attr.CacheRegion;
        }
        private static string BuildCacheKey(Type type, object key)
        {
            return string.Format("Entity:{0}:{1}", type.FullName, key.ToString());
        }
    }
}