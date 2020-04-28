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
        /// <param name="asset">Asset to be copied</param>
        /// <param name="destinationTemplate">Destination folder</param>
        /// <param name="verifySize">Whether to verify that new asset-size is allowed.</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="AssetFromOrigin"/> object representing copied file.</returns>
        public Task<AssetFromOrigin> CopyAssetToDisk(Asset asset, string destinationTemplate, bool verifySize,
            CancellationToken cancellationToken = default);
    }
}