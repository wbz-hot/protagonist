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

        public string GetUniqueName() => Id.Substring(Id.LastIndexOf('/') + 1);

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
    }
}
