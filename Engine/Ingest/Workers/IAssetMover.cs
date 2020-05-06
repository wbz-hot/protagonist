using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;

namespace Engine.Ingest.Workers
{
    public interface IAssetMover<T>
        where T : AssetFromOrigin
    {
        /// <summary>
        /// Copy specified asset from Origin to destination folder.
        /// </summary>
        /// <param name="asset">Asset to be copied</param>
        /// <param name="destination">Destination location.</param>
        /// <param name="verifySize">Whether to verify that new asset-size is allowed.</param>
        /// <param name="customerOriginStrategy">Customer origin strategy to use for fetching asset.</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="AssetFromOrigin"/> object representing copied file.</returns>
        public Task<T> CopyAsset(Asset asset, 
            string destination, 
            bool verifySize,
            CustomerOriginStrategy customerOriginStrategy,
            CancellationToken cancellationToken = default);
    }
}