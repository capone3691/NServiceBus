﻿namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;
    using Serialization;
    using Unicast.Messages;

    //todo: rename to LogicalOutgoingContext
    class SerializeMessageConnector : StageConnector<IOutgoingLogicalMessageContext, IOutgoingPhysicalMessageContext>
    {
        public SerializeMessageConnector(IMessageSerializer messageSerializer, MessageMetadataRegistry messageMetadataRegistry)
        {
            this.messageSerializer = messageSerializer;
            this.messageMetadataRegistry = messageMetadataRegistry;
        }

        public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingPhysicalMessageContext, Task> stage)
        {
            if (log.IsDebugEnabled)
            {
                log.DebugFormat("Serializing message '{0}' with id '{1}', ToString() of the message yields: {2} \n",
                    context.Message.MessageType != null ? context.Message.MessageType.AssemblyQualifiedName : "unknown",
                    context.MessageId, context.Message.Instance);
            }

            if (context.ShouldSkipSerialization())
            {
                await stage(this.CreateOutgoingPhysicalMessageContext(new byte[0], context.RoutingStrategies, context)).ConfigureAwait(false);
                return;
            }

            context.Headers[Headers.ContentType] = messageSerializer.ContentType;
            context.Headers[Headers.EnclosedMessageTypes] = SerializeEnclosedMessageTypes(context.Message.MessageType);

            var array = Serialize(context);
            await stage(this.CreateOutgoingPhysicalMessageContext(array, context.RoutingStrategies, context)).ConfigureAwait(false);
        }

        byte[] Serialize(IOutgoingLogicalMessageContext context)
        {
            using (var ms = new MemoryStream())
            {
                messageSerializer.Serialize(context.Message.Instance, ms);
                return ms.ToArray();
            }
        }

        string SerializeEnclosedMessageTypes(Type messageType)
        {
            var metadata = messageMetadataRegistry.GetMessageMetadata(messageType);
            var distinctTypes = metadata.MessageHierarchy.Distinct();
            return string.Join(";", distinctTypes.Select(t => t.AssemblyQualifiedName));
        }

        MessageMetadataRegistry messageMetadataRegistry;
        IMessageSerializer messageSerializer;

        static ILog log = LogManager.GetLogger<SerializeMessageConnector>();
    }
}