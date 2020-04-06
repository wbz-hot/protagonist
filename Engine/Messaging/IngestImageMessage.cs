using System;
using System.Collections.Generic;
using JustSaying.Models;
using Newtonsoft.Json;

namespace Engine.Messaging
{
    public class IngestImageMessage : Message, IMessagingEvent
    {
        [JsonProperty("__type")] 
        public string Type { get; set; }
        
        [JsonProperty("__created")]
        public DateTime Created { get; set; }
        
        public string Message { get; set; }
        
        public Dictionary<string, string> Params { get; set; }
    }
}