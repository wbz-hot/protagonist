using System;

namespace DLCS.Repository.Entities
{
    /// <summary>
    /// Represents Asset entity as stored in DLCS database.
    /// </summary>
    internal class AssetEntity
    {
        public string Id { get; set; }
        public int Customer { get; set; }
        public int Space { get; set; }
        public DateTime Created { get; set; }
        public string Origin { get; set; }
        
        public string Tags { get; set; }
        
        public string Roles { get; set; }
        public string PreservedUri { get; set; }
        
        public string Reference1 { get; set; }
        
        public string Reference2 { get; set; }
        
        public string Reference3 { get; set; }
        
        public int NumberReference1 { get; set; }
        
        public int NumberReference2 { get; set; }
        
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
    }
}