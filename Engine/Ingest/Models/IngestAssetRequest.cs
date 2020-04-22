using System;
using DLCS.Model.Assets;

namespace Engine.Ingest.Models
{
    // NOTE: When the API is rewritten it can ommit a message that is compatible with this.
    /// <summary>
    /// Represents a request to ingest an asset.
    /// </summary>
    public class IngestAssetRequest
    {
        /// <summary>
        /// Get date that this request was created.
        /// </summary>
        public DateTime? Created { get; }
        
        /// <summary>
        /// Get Asset to be ingested.
        /// </summary>
        public Asset Asset { get; }

        public IngestAssetRequest(Asset asset, DateTime? created)
        {
            Asset = asset;
            Created = created;
        }
    }
}