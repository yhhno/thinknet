﻿using System;
using System.Collections.Generic;
using System.Linq;
using ThinkLib.Logging;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    [RegisterComponent(typeof(ICommandBus))]
    public class CommandBus : AbstractBus, ICommandBus
    {
        private readonly IMessageSender messageSender;
        //private readonly ICommandResultManager commandResultManager;
        private readonly IRoutingKeyProvider routingKeyProvider;
        private readonly IMetadataProvider metadataProvider;
        private readonly ILogger logger;

        public CommandBus(IMessageSender messageSender,
            //ICommandResultManager commandResultManager,
            IRoutingKeyProvider routingKeyProvider,
            IMetadataProvider metadataProvider)
        {
            this.messageSender = messageSender;
            //this.commandResultManager = commandResultManager;
            this.routingKeyProvider = routingKeyProvider;
            this.metadataProvider = metadataProvider;
            this.logger = LogManager.GetLogger("ThinkNet");
        }

        protected override bool SearchMatchType(Type type)
        {
            return TypeHelper.IsCommand(type);
        }
        //public Task<CommandResult> Send(ICommand command, CommandReplyType commandReplyType)
        //{
        //    var task = commandResultManager.RegisterCommand(command, commandReplyType);

        //    this.Send(command);

        //    return task;
        //}

        public void Send(ICommand command)
        {
            this.Send(new[] { command });
        }

        public void Send(IEnumerable<ICommand> commands)
        {
            var messages = commands.Select(Map).AsEnumerable();
            messageSender.SendAsync(messages, () => {
                if (logger.IsDebugEnabled) {
                    logger.DebugFormat("command sended. commands:{0}.", string.Join(",", commands));
                }
            }, (ex) => {
            });        
        }


        private Message Map(ICommand command)
        {
            return new Message {
                Body = command,
                MetadataInfo = metadataProvider.GetMetadata(command),
                RoutingKey = routingKeyProvider.GetRoutingKey(command),
                CreatedTime = DateTime.UtcNow
            };
        }
    }
}
