using System.Threading.Tasks;

namespace DLCS.Model.Assets
{
    public interface IAssetRepository
    {
        public Task<Asset> GetAsset(string id);

        /// <summary>
        /// Marks an asset as ingested - updating Images, Batch, ImageLocation and ImageStorage.
        /// </summary>
        public Task<bool> UpdateIngestedAsset(Asset asset, ImageLocation? imageLocation, ImageStorage imageStorage);
    }
}
