using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;

namespace Engine.Ingest.Workers
{
    // TODO - name this better
    public interface IAssetFetcher
    {
        public Task<AssetFromOrigin> CopyAssetFromOrigin(Asset asset, string destinationFolder, CancellationToken cancellationToken);
    }

    /// <summary>
    /// An asset that has been copied from Origin.
    /// </summary>
    public class AssetFromOrigin
    {
        /// <summary>
        /// The DLCS asset id.
        /// </summary>
        public string AssetId { get; }
        public long AssetSize { get; }
        public string LocationOnDisk { get; }
        
        // TODO - should this contain asset type too? (e.g. image/jpg)

        public AssetFromOrigin(string assetId, long assetSize, string locationOnDisk)
        {
            AssetId = assetId;
            AssetSize = assetSize;
            LocationOnDisk = locationOnDisk;
        }
    }
}