using System;
using System.Collections.Generic;
using System.Linq;
using DLCS.Core.Guard;
using DLCS.Model.Converters;
using Newtonsoft.Json;

namespace DLCS.Model.Assets
{
    /// <summary>
    /// Represents an Asset used by DLCS system.
    /// </summary>
    public class Asset
    {
        public string Id { get; set; }
        public int Customer { get; set; }
        public int Space { get; set; }
        public DateTime Created { get; set; }
        
        /// <summary>
        /// Origin to use for first ingestion only.
        /// </summary>
        public string InitialOrigin { get; set; }
        public string Origin { get; set; }
        
        public List<string> Tags { get; set; }
        
        public List<string> Roles { get; set; }
        public string PreservedUri { get; set; }
        
        public string String1 { get; set; }
        
        public string String2 { get; set; }
        
        public string String3 { get; set; }
        
        public int Number1 { get; set; }
        
        public int Number2 { get; set; }
        
        public int Number3 { get; set; }
        
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
        
        [JsonConverter(typeof(AssetFamilyConverter))]
        public AssetFamily Family { get; set; }
        public string MediaType { get; set; }
        public long Duration { get; set; }
        
        public ThumbnailPolicy FullThumbnailPolicy { get; private set; }
        
        public ImageOptimisationPolicy FullImageOptimisationPolicy { get; private set; }
        
        /// <summary>
        /// Get value indicating if this Asset is in an Error state.
        /// </summary>
        public bool HasError => !string.IsNullOrWhiteSpace(Error);

        private string uniqueName;

        /// <summary>
        /// Get the identifier part from from Id.
        /// Id contains {cust}/{space}/{identifier}
        /// </summary>
        /// <returns></returns>
        public string GetUniqueName()
        {
            if (string.IsNullOrWhiteSpace(uniqueName))
            {
                uniqueName = Id.Substring(Id.LastIndexOf('/') + 1);
            }

            return uniqueName;
        } 

        public Asset WithThumbnailPolicy(ThumbnailPolicy thumbnailPolicy)
        {
            FullThumbnailPolicy = thumbnailPolicy.ThrowIfNull(nameof(thumbnailPolicy));
            return this;
        }
        
        public Asset WithImageOptimisationPolicy(ImageOptimisationPolicy imageOptimisationPolicy)
        {
            FullImageOptimisationPolicy = imageOptimisationPolicy.ThrowIfNull(nameof(imageOptimisationPolicy));
            return this;
        }

        public Asset MarkAsIngestComplete()
        {
            Finished = DateTime.Now; // TODO should be DateTime.UtcNow
            Ingesting = false;
            return this;
        }

        /// <summary>
        /// Get origin to use for ingestion. This will be 'initialOrigin' if present, else origin.
        /// </summary>
        public string GetIngestOrigin()
            => string.IsNullOrWhiteSpace(InitialOrigin) ? Origin : InitialOrigin;
    }
}
