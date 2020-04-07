using System;
using System.Threading.Tasks;
using Engine.Messaging;
using JustSaying.Messaging.MessageHandling;

namespace Engine.Ingest
{
    public class IngestMessageHandler : IHandlerAsync<IngestImageMessage>
    {
        public Task<bool> Handle(IngestImageMessage message)
        {
            throw new NotImplementedException();
        }
    }
}