using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;

namespace Engine.Ingest.Workers
{
    // TODO - name this better.
    // does this need to exist?
    public interface IAssetFetcher
    {
        public Task<AssetFromOrigin> CopyAssetFromOrigin(Asset asset, string destinationFolder, CancellationToken cancellationToken);
    }
}