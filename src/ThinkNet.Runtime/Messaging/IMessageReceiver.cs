﻿using System;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    public interface IMessageReceiver
    {
        /// <summary>
        /// Event raised whenever a message is received. Consumer of the event is responsible for disposing the message when appropriate.
        /// </summary>
        event EventHandler<EventArgs<Message>> MessageReceived;

        /// <summary>
        /// Starts the listener.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the listener.
        /// </summary>
        void Stop();
    }
}
