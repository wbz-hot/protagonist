using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Dapper;
using DLCS.Model;
using DLCS.Model.Assets;
using DLCS.Repository.Entities;
using LazyCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DLCS.Repository.Assets
{
    public class AssetPolicyRepository : IAssetPolicyRepository
    {
        private readonly IAppCache appCache;
        private readonly ILogger<AssetPolicyRepository> logger;
        private readonly DatabaseAccessor databaseAccessor;
        private static readonly int AvailablePolicies = Enum.GetValues(typeof(AssetPolicies)).Length - 1;

        public AssetPolicyRepository(IAppCache appCache,
            ILogger<AssetPolicyRepository> logger,
            DatabaseAccessor databaseAccessor)
        {
            this.appCache = appCache;
            this.logger = logger;
            this.databaseAccessor = databaseAccessor;
        }

        public async Task<ThumbnailPolicy> GetThumbnailPolicy(string thumbnailPolicyId)
        {
            try
            {
                var thumbnailPolicies = await GetThumbnailPolicies();
                return thumbnailPolicies.SingleOrDefault(p => p.Id == thumbnailPolicyId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error getting thumbnailPolicy with id {thumbnailPolicyId}",
                    thumbnailPolicyId);
                return null;
            }
        }

        public async Task<ImageOptimisationPolicy> GetImageOptimisationPolicy(string imageOptimisationPolicyId)
        {
            try
            {
                var thumbnailPolicies = await GetImageOptimisationPolicies();
                return thumbnailPolicies.SingleOrDefault(p => p.Id == imageOptimisationPolicyId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error getting imageOptimisationPolicy with id {imageOptimisationPolicyId}",
                    imageOptimisationPolicyId);
                return null;
            }
        }

        public async Task HydratePolicies(Asset asset, AssetPolicies policiesToSet)
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

        private async Task<List<ThumbnailPolicy>> GetThumbnailPolicies()
        {
            const string key = "AssetPolicyRepository_ThumbnailPolicies";
            return await appCache.GetOrAddAsync(key, entry =>
            {
                // TODO - config
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                logger.LogInformation("Refreshing ThumbnailPolicies from database");
                const string policiesQuery = "SELECT \"Id\", \"Name\", \"Sizes\" FROM \"ThumbnailPolicies\"";
                return databaseAccessor.SelectAndMapList<ThumbnailPolicyEntity, ThumbnailPolicy>(policiesQuery);
            });
        }

        private async Task<List<ImageOptimisationPolicy>> GetImageOptimisationPolicies()
        {
            const string key = "AssetPolicyRepository_ImageOptimisation";
            return await appCache.GetOrAddAsync(key, entry =>
            {
                // TODO - config
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                logger.LogInformation("Refreshing ThumbnailPolicies from database");
                const string policiesQuery =
                    "SELECT \"Id\", \"Name\", \"TechnicalDetails\" FROM \"ImageOptimisationPolicies\"";
                return databaseAccessor.SelectAndMapList<ImageOptimisationPolicyEntity, ImageOptimisationPolicy>(policiesQuery);
            });
        }
    }
}