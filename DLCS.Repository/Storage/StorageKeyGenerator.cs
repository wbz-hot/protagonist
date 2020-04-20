using DLCS.Model.Assets;
using IIIF;
using thumbConsts =  DLCS.Repository.Settings.ThumbsSettings.Constants;

namespace DLCS.Repository.Storage
{
    public static class StorageKeyGenerator
    {
        /// <summary>
        /// Get the storage key for specified space/customer/key
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
        
        /// <summary>
        /// Get path for 'low.jpg' thumbnail ({key}/low.jpg)
        /// </summary>
        public static string GetLowPath(string key)
            => string.Concat(key, "low.jpg");

        /// <summary>
        /// Get path for "w,h" thumbnail ({key}/full/w,h/0/default.jpg
        /// </summary>
        public static string GetThumbnailWHPath(string key, Size size)
            => $"{key}full/{size.Width},{size.Height}/0/default.jpg";

        /// <summary>
        /// Get path for "w," thumbnail ({key}/full/w,/0/default.jpg
        /// </summary>
        public static string GetThumbnailWPath(string key, Size size)
            => $"{key}full/{size.Width},/0/default.jpg";

        /// <summary>
        /// Get path for confined square thumbnail
        /// ({key}/open/100.jpg or {key}/auth/100.jpg) 
        /// </summary>
        public static string GetConfinedSquarePath(string key, int largestSize, bool isOpen)
        {
            var slug = isOpen ? thumbConsts.OpenSlug : thumbConsts.AuthorisedSlug;
            return $"{key}{slug}/{largestSize}.jpg";
        }

        /// <summary>
        /// Get path for confined square thumbnail
        /// ({key}/open/100.jpg or {key}/auth/100.jpg) 
        /// </summary>
        public static string GetConfinedSquarePath(string key, Size size, bool isOpen)
            => GetConfinedSquarePath(key, size.MaxDimension, isOpen);
        
        /// <summary>
        /// Get path for s.json file. ({key}/s.json) 
        /// </summary>
        public static string GetSizesJsonPath(string key)
            => string.Concat(key, thumbConsts.SizesJsonKey);
    }
}