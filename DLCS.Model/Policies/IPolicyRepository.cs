using System;
using System.Threading.Tasks;
using DLCS.Model.Assets;

namespace DLCS.Model.Policies
{
    public interface IPolicyRepository
    {
        /// <summary>
        /// Get ThumbnailPolicy with specified Id.
        /// </summary>
        Task<ThumbnailPolicy?> GetThumbnailPolicy(string thumbnailPolicyId);

        /// <summary>
        /// Get ImageOptimisationPolicy with specified Id.
        /// </summary>
        Task<ImageOptimisationPolicy?> GetImageOptimisationPolicy(string imageOptimisationPolicyId);

        /// <summary>
        /// Get StoragePolicy with specified Id.
        /// </summary>
        Task<StoragePolicy?> GetStoragePolicy(string storagePolicyId);

        /// <summary>
        /// Update provided Asset with policies as set out by policiesToSet param.
        /// </summary>
        Task HydrateAssetPolicies(Asset asset, AssetPolicies policiesToSet);
    }
    
    /// <summary>
    /// Enumeration of possible policies that can be set on Asset.
    /// </summary>
    [Flags]
    public enum AssetPolicies
    {
        None = 0,
        Thumbnail = 1,
        ImageOptimisation = 2,
        All = Thumbnail | ImageOptimisation
    }
}