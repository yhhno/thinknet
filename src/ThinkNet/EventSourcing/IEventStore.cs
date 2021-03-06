﻿using System;
using System.Collections.Generic;

namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 事件存储。
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// 保存溯源事件。如果该命令产生的事件已保存过则为false，否则为true
        /// </summary>
        bool Save(SourceKey sourceKey, string correlationId, Func<IEnumerable<Stream>> eventsFactory);

        ///// <summary>
        ///// 判断该命令下是否存在相关事件。
        ///// </summary>
        //bool EventPersisted(SourceKey sourceKey, string correlationId);

        /// <summary>
        /// 查询该命令下的事件。
        /// </summary>
        IEnumerable<Stream> FindAll(SourceKey sourceKey, string correlationId);
        /// <summary>
        /// 查询聚合的溯源事件。
        /// </summary>
        IEnumerable<Stream> FindAll(SourceKey sourceKey, int version);

        /// <summary>
        /// 移除该聚合的溯源事件。
        /// </summary>
        void RemoveAll(SourceKey sourceKey);
    }    
}
