﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;


namespace ThinkNet.Kernel
{
    /// <summary>
    /// 实现 <see cref="IAggregateRoot"/> 的抽象类
    /// </summary>
    [DataContract]
    [Serializable]
    public abstract class AggregateRoot<TIdentify> : Entity<TIdentify>, IEventSourced, IEventPublisher, ICloneable
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        protected AggregateRoot()
        { }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected AggregateRoot(TIdentify id)
            : base(id)
        { }

        /// <summary>
        /// 版本号
        /// </summary>
        public int Version { get; private set; }


        [NonSerialized]
        private ICollection<IEvent> pendingEvents;
        /// <summary>
        /// 引发事件并将其加入到待发布事件列表
        /// </summary>
        protected void RaiseEvent<TEvent>(TEvent @event)
            where TEvent : IEvent
        {
            var domainEvent = @event as Event<TIdentify>;
            var versionedEvent = @event as VersionedEvent<TIdentify>;

            if (domainEvent != null)
                domainEvent.SourceId = this.Id;
            if (versionedEvent!=null)
                versionedEvent.Version = this.Version + 1;
            this.Handling(@event);
            if (versionedEvent != null)
                this.Version = versionedEvent.Version;

            if (pendingEvents == null) {
                pendingEvents = new List<IEvent>();
            }
            pendingEvents.Add(@event);
        }

        private void Handling(IEvent @event)
        {
            var eventType = @event.GetType();
            var aggregateRootType = this.GetType();
            var handler = AggregateRootInnerHandlerUtil.GetEventHandler(aggregateRootType, eventType);
            if (handler == null) {
                // TODO..警告

                if (@event is IVersionedEvent)
                    throw new EventHandlerNotFoundException(aggregateRootType, eventType);
            }
            handler(this, @event);
        }

        /// <summary>
        /// 获取待发布的事件列表。
        /// </summary>
        protected IEnumerable<IEvent> GetPendingEvents()
        {
            if (pendingEvents == null) {
                pendingEvents = new List<IEvent>();
            }
            return this.pendingEvents;
        }

        /// <summary>
        /// 清除事件。
        /// </summary>
        protected void ClearEvents()
        {
            if (pendingEvents != null) {
                pendingEvents.Clear();
                pendingEvents = null;
            }
        }

        public TRole ActAs<TRole>() where TRole : class
        {
            if (!typeof(TRole).IsInterface) {
                throw new AggregateRootException(string.Format("'{0}' is not an interface type.", typeof(TRole).FullName));
            }

            var actor = this as TRole;

            if (actor == null) {
                throw new AggregateRootException(string.Format("'{0}' cannot act as role '{1}'.", GetType().FullName, typeof(TRole).FullName));
            }

            return actor;
        }

        protected virtual object Clone()
        {
            return null;
        }



        #region IEventSourced 成员

        IEnumerable<IVersionedEvent> IEventSourced.GetEvents()
        {
            throw new NotImplementedException();
        }

        private void CheckEvent(IVersionedEvent @event)
        {
            if (@event.Version == 1 && this.Id.Equals(default(TIdentify)))
                this.Id = (TIdentify)TypeDescriptor.GetConverter(typeof(TIdentify)).ConvertFromString(@event.SourceId);

            if (@event.Version > 1 && this.Id.ToString() != @event.SourceId)
                throw new EventSourcedException(@event.SourceId, this.Id.ToString());

            if (@event.Version != this.Version + 1)
                throw new EventSourcedException(@event.Version, this.Version);
        }        

        void IEventSourced.LoadFrom(IEnumerable<IVersionedEvent> events)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IAggregateRoot 成员

        [IgnoreDataMember]
        object IAggregateRoot.Id
        {
            get
            {
                return this.Id;
            }
        }

        #endregion

        #region IEventPublisher 成员
        [IgnoreDataMember]
        IEnumerable<IEvent> IEventPublisher.Events
        {
            get
            {
                return this.GetPendingEvents();
            }
        }

        #endregion

        #region ICloneable 成员

        object ICloneable.Clone()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}