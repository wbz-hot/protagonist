using System.Threading.Tasks;

namespace DLCS.Model.Assets
{
    public interface IAssetRepository
    {
        public Task<Asset> GetAsset(string id);

        public Task<bool> UpdateAsset(Asset asset, ImageLocation imageLocation, ImageStorage imageStorage);
    }
}
