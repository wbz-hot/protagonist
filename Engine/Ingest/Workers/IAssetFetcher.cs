using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;

namespace Engine.Ingest.Workers
{
    // TODO - name this better
    public interface IAssetFetcher
    {
        public Task CopyAssetFromOrigin(Asset asset, string destinationFolder, CancellationToken cancellationToken);
    }
}