﻿using System;
using System.Collections;
using System.Data;
using System.Linq;

using ThinkLib.Common;
using ThinkLib.Contexts;


namespace ThinkNet.Database
{
    /// <summary>
    /// 实现 <see cref="IDataContext"/> 的抽象类
    /// </summary>
    public abstract class DataContextBase : DisposableObject, IDataContext, IContext
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        protected DataContextBase()
        { }
        private readonly IContextManager _contextManager;
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected DataContextBase(IContextManager contextManager)
        {
            this._contextManager = contextManager;
        }

        /// <summary>
        /// 当前的数据连接
        /// </summary>
        public abstract IDbConnection DbConnection { get; }

        private void Validate(object entity)
        {
            var validation = entity as IValidatable<IDataContext>;
            if (validation != null) {
                validation.Validate(this);
            }
        }
        private LifecycleVeto Callback(object entity, Func<ILifecycle<IDataContext>, IDataContext, LifecycleVeto> action)
        {
            var lifecycle = entity as ILifecycle<IDataContext>;
            if (lifecycle != null) {
                return action(lifecycle, this);
            }
            return LifecycleVeto.Accept;
        }
        private static LifecycleVeto OnSaving(ILifecycle<IDataContext> entity, IDataContext context)
        {
            return (entity as ILifecycle<IDataContext>).OnSaving(context);
        }
        private static LifecycleVeto OnUpdating(ILifecycle<IDataContext> entity, IDataContext context)
        {
            return (entity as ILifecycle<IDataContext>).OnUpdating(context);
        }
        private static LifecycleVeto OnDeleting(ILifecycle<IDataContext> entity, IDataContext context)
        {
            return (entity as ILifecycle<IDataContext>).OnDeleting(context);
        }
       

        /// <summary>
        /// 预处理事务。
        /// </summary>
        protected abstract void DoCommit();


        /// <summary>
        /// 获取跟踪的对象集合
        /// </summary>
        public abstract ICollection TrackingObjects { get; }


        /// <summary>
        /// 提交事务。
        /// </summary>
        public void Commit()
        {
            this.DoCommit();

            this.DataCommitted(this, EventArgs.Empty);
        }


        protected virtual void SaveOrUpdate(object entity, Func<object, bool> beforeSave, Func<object, bool> beforeUpdate)
        {
            if (this.Contains(entity)) {
                this.Update(entity, beforeUpdate);
            }
            else {
                this.Save(entity, beforeSave);
            }
        }

        public void SaveOrUpdate(object entity)
        {
            this.Validate(entity);

            this.SaveOrUpdate(entity,
                (state) => this.Callback(state, OnSaving) == LifecycleVeto.Accept,
                (state) => this.Callback(state, OnUpdating) == LifecycleVeto.Accept);
        }

        
        protected abstract void Save(object entity, Func<object, bool> beforeSave);
        /// <summary>
        /// 新增一个新对象到当前上下文
        /// </summary>
        public void Save(object entity)
        {
            this.Validate(entity);

            this.Save(entity,
                (state) => this.Callback(state, OnSaving) == LifecycleVeto.Accept);
        }

        
        protected abstract void Update(object entity, Func<object, bool> beforeUpdate);

        /// <summary>
        /// 修改一个对象到当前上下文
        /// </summary>
        public void Update(object entity)
        {
            this.Validate(entity);

            this.Update(entity,
                (state) => this.Callback(state, OnUpdating) == LifecycleVeto.Accept);
        }
        
        protected abstract void Delete(object entity, Func<object, bool> beforeDelete);
        /// <summary>
        /// 删除一个对象到当前上下文
        /// </summary>
        public void Delete(object entity)
        {
            this.Delete(entity,
                (state) => this.Callback(state, OnDeleting) == LifecycleVeto.Accept);
        }

        /// <summary>
        /// 当前工作单元是否包含该对象
        /// </summary>
        public abstract bool Contains(object entity);
        /// <summary>
        /// 从当前工作分离该对象
        /// </summary>
        public abstract void Detach(object entity);

        /// <summary>
        /// 将该对象附加到当前上下文中
        /// </summary>
        public abstract void Attach(object entity);

        /// <summary>
        /// 从数据库刷新最新状态
        /// </summary>
        public abstract void Refresh(object entity);
        
        /// <summary>
        /// 获取实例信息
        /// </summary>
        public abstract object Find(Type type, object id);

        /// <summary>
        /// 获取实例信息
        /// </summary>
        public abstract object Find(Type type, params object[] keyValues);

        /// <summary>
        /// 加载实例信息
        /// </summary>
        public abstract void Load(object entity);
        void IDataContext.Load(object entity)
        {
            entity.NotNull("entity");

            this.Load(entity);
            if (entity is ILifecycle<IDataContext>) {
                (entity as ILifecycle<IDataContext>).OnLoaded(this);
            }
        }

        /// <summary>
        /// 获取对数据类型已知的特定数据源的查询进行计算的功能。
        /// </summary>
        public abstract IQueryable<TEntity> CreateQuery<TEntity>() where TEntity : class;
        

        /// <summary>
        /// 在数据提交成功后执行
        /// </summary>
        public event EventHandler DataCommitted = (sender, args) => { };

        IContextManager IContext.ContextManager
        {
            get { return this._contextManager; }
        }

    }
}
