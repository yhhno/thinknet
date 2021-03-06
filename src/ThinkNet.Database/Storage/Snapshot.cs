﻿using System;
using System.Linq;

namespace ThinkNet.Database.Storage
{
    /// <summary>
    /// 聚合快照
    /// </summary>
    [Serializable]
    public class Snapshot
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Snapshot()
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public Snapshot(int aggregateRootTypeCode, string aggregateRootId)
        {
            this.AggregateRootId = aggregateRootId;
            this.AggregateRootTypeCode = aggregateRootTypeCode;
        }

        /// <summary>
        /// 聚合根标识
        /// </summary>
        public string AggregateRootId { get; set; }
        /// <summary>
        /// 聚合根类型名称
        /// </summary>
        public int AggregateRootTypeCode { get; set; }
        /// <summary>
        /// 创建该聚合快照的聚合根版本号
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// 程序集
        /// </summary>
        public string AssemblyName { get; set; }
        /// <summary>
        /// 程序集
        /// </summary>
        public string Namespace { get; set; }
        /// <summary>
        /// 类型名称
        /// </summary>
        public string TypeName { get; set; }
        /// <summary>
        /// 聚合根数据
        /// </summary>
        public byte[] Data { get; set; }
        /// <summary>
        /// 创建该快照的时间
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 返回此实例的哈希代码
        /// </summary>
        public override int GetHashCode()
        {
            return new int[] {
                AggregateRootTypeCode.GetHashCode(),
                AggregateRootId.GetHashCode()
            }.Aggregate((x, y) => x ^ y);
        }

        /// <summary>
        /// 确定此实例是否与指定的对象（也必须是 <see cref="Snapshot"/> 对象）相同。
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as Snapshot;
            if (other == null) {
                return false;
            }

            return other.AggregateRootTypeCode == this.AggregateRootTypeCode
                && other.AggregateRootId == this.AggregateRootId;
        }

        /// <summary>
        /// 将此实例的标识转换为其等效的字符串表示形式。
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}_{1}", AggregateRootTypeCode, AggregateRootId);
        }
    }
}
