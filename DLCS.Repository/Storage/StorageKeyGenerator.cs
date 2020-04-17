using DLCS.Model.Assets;

namespace DLCS.Repository.Storage
{
    public static class StorageKeyGenerator
    {
        /// <summary>
        /// Get the storage get for specified space/customer/key
        /// </summary>
        /// <param name="customer">Customer Id.</param>
        /// <param name="space">Space id.</param>
        /// <param name="assetKey">Unique Id of the asset.</param>
        /// <returns>/customer/space/imageKey string.</returns>
        public static string GetStorageKey(int customer, int space, string assetKey)
            => $"{customer}/{space}/{assetKey}";
        
        /// <summary>
        /// Get the storage get for specified space/customer/key
        /// </summary>
        /// <param name="asset">Asset to get storage key for.</param>
        /// <returns>/customer/space/imageKey string.</returns>
        public static string GetStorageKey(this Asset asset)
            => GetStorageKey(asset.Customer, asset.Space, asset.GetUniqueName());
    }
}