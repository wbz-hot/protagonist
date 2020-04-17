using DLCS.Model.Customer;

namespace Engine.Ingest.Workers
{
    /// <summary>
    /// An asset that has been copied from Origin.
    /// </summary>
    public class AssetFromOrigin
    {
        /// <summary>
        /// The DLCS asset id.
        /// </summary>
        public string AssetId { get; }
        
        /// <summary>
        /// The size of the asset.
        /// </summary>
        public long AssetSize { get; }
        
        /// <summary>
        /// The current location of the asset on scratch disk.
        /// </summary>
        public string LocationOnDisk { get; set; }
        
        /// <summary>
        /// The current location of the asset on scratch disk.
        /// </summary>
        /// <remarks>This is required for calling Tizer/Appetiser.</remarks>
        public string RelativeLocationOnDisk { get; set; }
        
        /// <summary>
        /// The type of the asset.
        /// </summary>
        public string ContentType { get; }
        
        /// <summary>
        /// The customer origin strategy used to process this asset.
        /// </summary>
        public CustomerOriginStrategy CustomerOriginStrategy { get; set; }

        // TODO - for ContentType - can the type of the Asset be used here?

        public AssetFromOrigin(string assetId, long assetSize, string locationOnDisk, string contentType)
        {
            AssetId = assetId;
            AssetSize = assetSize;
            LocationOnDisk = locationOnDisk;
            ContentType = contentType;
        }
    }
}