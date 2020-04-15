using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;

namespace Engine.Ingest.Workers
{
    // TODO - name this better
    public interface IAssetFetcher
    {
        public Task<FetchedAsset> CopyAssetFromOrigin(Asset asset, string destinationFolder, CancellationToken cancellationToken);
    }

    public class FetchedAsset
    {
        public long AssetSize { get; }
        public string LocationOnDisk { get; }
        
        // TODO - should this contain asset type too?

        public FetchedAsset(long assetSize, string locationOnDisk)
        {
            AssetSize = assetSize;
            LocationOnDisk = locationOnDisk;
        }
    }
}