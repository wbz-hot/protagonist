using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using DLCS.Model.Assets;
using DLCS.Model.Policies;
using DLCS.Repository.Entities;
using LazyCache;
using Microsoft.Extensions.Logging;

namespace DLCS.Repository.Policies
{
    public class PolicyRepository : IPolicyRepository
    {
        private readonly IAppCache appCache;
        private readonly ILogger<PolicyRepository> logger;
        private readonly DatabaseAccessor databaseAccessor;
        private static readonly int AvailablePolicies = Enum.GetValues(typeof(AssetPolicies)).Length - 1;

        public PolicyRepository(IAppCache appCache,
            ILogger<PolicyRepository> logger,
            DatabaseAccessor databaseAccessor)
        {
            this.appCache = appCache;
            this.logger = logger;
            this.databaseAccessor = databaseAccessor;
        }

        public async Task<ThumbnailPolicy?> GetThumbnailPolicy(string thumbnailPolicyId)
        {
            try
            {
                var thumbnailPolicies = await GetThumbnailPolicies();
                return thumbnailPolicies.SingleOrDefault(p => p.Id == thumbnailPolicyId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error getting ThumbnailPolicy with id {thumbnailPolicyId}",
                    thumbnailPolicyId);
                return null;
            }
        }

        public async Task<ImageOptimisationPolicy?> GetImageOptimisationPolicy(string imageOptimisationPolicyId)
        {
            try
            {
                var thumbnailPolicies = await GetImageOptimisationPolicies();
                return thumbnailPolicies.SingleOrDefault(p => p.Id == imageOptimisationPolicyId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error getting ImageOptimisationPolicy with id {imageOptimisationPolicyId}",
                    imageOptimisationPolicyId);
                return null;
            }
        }

        public async Task<StoragePolicy?> GetStoragePolicy(string storagePolicyId)
        {
            try
            {
                var thumbnailPolicies = await GetStoragePolicies();
                return thumbnailPolicies.SingleOrDefault(p => p.Id == storagePolicyId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error getting StoragePolicy with id {storagePolicyId}",
                    storagePolicyId);
                return null;
            }
        }

        public async Task HydrateAssetPolicies(Asset asset, AssetPolicies policiesToSet)
        {
            var getPolicyRequests = new List<Task>(AvailablePolicies);
            if (policiesToSet.HasFlag(AssetPolicies.Thumbnail))
            {
                var getThumbnail = GetThumbnailPolicy(asset.ThumbnailPolicy)
                    .ContinueWith(
                        task => asset.WithThumbnailPolicy(task.Result),
                        TaskContinuationOptions.OnlyOnRanToCompletion);
                getPolicyRequests.Add(getThumbnail);
            }

            if (policiesToSet.HasFlag(AssetPolicies.ImageOptimisation))
            {
                var getThumbnail = GetImageOptimisationPolicy(asset.ImageOptimisationPolicy)
                    .ContinueWith(
                        task => asset.WithImageOptimisationPolicy(task.Result),
                        TaskContinuationOptions.OnlyOnRanToCompletion);
                getPolicyRequests.Add(getThumbnail);
            }

            await Task.WhenAll(getPolicyRequests);
        }
        
        private Task<List<StoragePolicy>> GetStoragePolicies()
        {
            const string key = "PolicyRepository_StoragePolicies";
            return appCache.GetOrAddAsync(key, async entry =>
            {
                // TODO - config
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                logger.LogInformation("Refreshing StoragePolicies from database");
                const string policiesQuery = "SELECT \"Id\", \"MaximumNumberOfStoredImages\", \"MaximumTotalSizeOfStoredImages\" FROM \"StoragePolicies\"";
                await using var connection = await databaseAccessor.GetOpenDbConnection();
                return (await connection.QueryAsync<StoragePolicy>(policiesQuery)).ToList();
            });
        }

        private Task<List<ThumbnailPolicy>> GetThumbnailPolicies()
        {
            const string key = "PolicyRepository_ThumbnailPolicies";
            return appCache.GetOrAddAsync(key, entry =>
            {
                // TODO - config
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                logger.LogInformation("Refreshing ThumbnailPolicies from database");
                const string policiesQuery = "SELECT \"Id\", \"Name\", \"Sizes\" FROM \"ThumbnailPolicies\"";
                return databaseAccessor.SelectAndMapList<ThumbnailPolicyEntity, ThumbnailPolicy>(policiesQuery);
            });
        }

        private Task<List<ImageOptimisationPolicy>> GetImageOptimisationPolicies()
        {
            const string key = "PolicyRepository_ImageOptimisation";
            return appCache.GetOrAddAsync(key, entry =>
            {
                // TODO - config
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                logger.LogInformation("Refreshing ImageOptimisationPolicies from database");
                const string policiesQuery =
                    "SELECT \"Id\", \"Name\", \"TechnicalDetails\" FROM \"ImageOptimisationPolicies\"";
                return databaseAccessor.SelectAndMapList<ImageOptimisationPolicyEntity, ImageOptimisationPolicy>(policiesQuery);
            });
        }
    }
}