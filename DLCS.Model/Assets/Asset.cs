using System;
using System.Collections.Generic;
using System.Linq;
using DLCS.Model.Converters;
using Newtonsoft.Json;

namespace DLCS.Model.Assets
{
    /// <summary>
    /// Represents an Asset that is stored in the DLCS database.
    /// </summary>
    public class Asset
    {
        public string Id { get; set; }
        public int Customer { get; set; }
        public int Space { get; set; }
        public DateTime Created { get; set; }
        public string Origin { get; set; }
        
        [JsonConverter(typeof(ArrayToStringConverter))]
        public string Tags { get; set; }
        
        [JsonConverter(typeof(ArrayToStringConverter))]
        public string Roles { get; set; }
        public string PreservedUri { get; set; }
        
        [JsonProperty("string1")]
        public string Reference1 { get; set; }
        
        [JsonProperty("string2")]
        public string Reference2 { get; set; }
        
        [JsonProperty("string3")]
        public string Reference3 { get; set; }
        
        [JsonProperty("number1")]
        public int NumberReference1 { get; set; }
        
        [JsonProperty("number2")]
        public int NumberReference2 { get; set; }
        
        [JsonProperty("number3")]
        public int NumberReference3 { get; set; }
        
        // -1 = null (all open), 0 = no allowed size without being auth
        public int MaxUnauthorised { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Error { get; set; }
        public int Batch { get; set; }
        public DateTime? Finished { get; set; }
        public bool Ingesting { get; set; }
        public string ImageOptimisationPolicy { get; set; }
        public string ThumbnailPolicy { get; set; }
        public string Family { get; set; }
        public string MediaType { get; set; }
        public long Duration { get; set; }

        private IEnumerable<string> rolesList = null;
        
        // TODO - map this via Dapper on way out of DB?
        public IEnumerable<string> RolesList
        {
            get
            {
                if (rolesList == null && !string.IsNullOrEmpty(Roles))
                {
                    rolesList = Roles.Split(",", StringSplitOptions.RemoveEmptyEntries); 
                }

                return rolesList;
            }
        }
    }
}
