﻿using ThinkNet.Common;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 存储处理程序信息的接口
    /// </summary>
    [RequiredComponent(typeof(HandlerRecordInMemory))]
    public interface IHandlerRecordStore
    {
        /// <summary>
        /// 添加处理程序信息
        /// </summary>
        void AddHandlerInfo(string messageId, string messageType, string handlerType);
        /// <summary>
        /// 检查该处理程序信息是否存在
        /// </summary>
        bool IsHandlerInfoExist(string messageId, string messageType, string handlerType);
    }
}