using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Engine.Messaging
{
    public class IngestImage : IMessagingEvent
    {
        [JsonProperty("_type")] 
        public string Type { get; set; }
        
        [JsonProperty("_created")]
        public DateTime Created { get; set; }
        
        public string Message { get; set; }
        
        public Dictionary<string, string> Params { get; set; }
    }
}