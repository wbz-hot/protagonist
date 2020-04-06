using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Engine.Messaging
{
    /// <summary>
    /// MessagingEvent passed to the Engine by DLCS API.
    /// </summary>
    /// <remarks>Legacy field from the Inversion framework.</remarks>
    public class MessagingEvent : IMessagingEvent
    {
        /// <summary>
        /// Gets the type of MessagingEvent.
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Gets the date this message was created.
        /// </summary>
        public DateTime Created { get; }
        
        /// <summary>
        /// Gets the type of this message.
        /// </summary>
        public string Message { get; }
        
        /// <summary>
        /// A collection of additional parameters associated with event. 
        /// </summary>
        /// <remarks>this could be an Asset but with stringX and numberX rather than ReferenceX and NumberReferenceX</remarks>
        public Dictionary<string, string> Params { get; }

        [JsonConstructor]
        public MessagingEvent(
            [JsonProperty("__type")] string type, 
            [JsonProperty("__created")] DateTime created,
            [JsonProperty("message")] string message, 
            [JsonProperty("params")] Dictionary<string, string> @params)
        {
            Type = type;
            Created = created;
            Message = message;
            Params = @params;
        }
    }
}