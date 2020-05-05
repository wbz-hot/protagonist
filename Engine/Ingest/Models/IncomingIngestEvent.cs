using System;
using System.Collections.Generic;
using DLCS.Model.Assets;
using Newtonsoft.Json;

namespace Engine.Ingest.Models
{
    /// <summary>
    /// Serialized Inversion MessagingEvent passed to the Engine by DLCS API.
    /// </summary>
    /// <remarks>Legacy fields from the Inversion framework.</remarks>
    public class IncomingIngestEvent
    {
        private const string AssetDictionaryKey = "image";
        
        /// <summary>
        /// Gets the type of MessagingEvent.
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Gets the date this message was created.
        /// </summary>
        public DateTime? Created { get; }
        
        /// <summary>
        /// Gets the type of this message.
        /// </summary>
        public string Message { get; }
        
        /// <summary>
        /// A collection of additional parameters associated with event. 
        /// </summary>
        public Dictionary<string, string> Params { get; }

        /// <summary>
        /// Serialized <see cref="Asset"/> as JSON.
        /// </summary>
        public string? AssetJson => Params.TryGetValue(AssetDictionaryKey, out var image) ? image : null;

        [JsonConstructor]
        public IncomingIngestEvent(
            [JsonProperty("_type")] string type, 
            [JsonProperty("_created")] DateTime? created,
            [JsonProperty("message")] string message, 
            [JsonProperty("params")] Dictionary<string, string> @params)
        {
            Type = type;
            Created = created;
            Message = message;
            Params = @params ?? new Dictionary<string, string>();
        }
    }
}