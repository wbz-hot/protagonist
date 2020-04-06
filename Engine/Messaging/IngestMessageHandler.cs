using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;

namespace Engine.Messaging
{
    public class IngestMessageHandler : IHandlerAsync<IngestImageMessage>
    {
        public Task<bool> Handle(IngestImageMessage message)
        {
            throw new NotImplementedException();
        }
    }
}