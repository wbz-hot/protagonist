using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;

namespace Engine.Ingest.Workers
{
    public interface IAssetFetcher
    {
        /// <summary>
        /// Copy specified asset from Origin to destination folder.
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="destinationFolder"></param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="AssetFromOrigin"/> object representing copied file.</returns>
        public Task<AssetFromOrigin> CopyAssetFromOrigin(Asset asset, string destinationFolder,
            CancellationToken cancellationToken = default);
    }
}