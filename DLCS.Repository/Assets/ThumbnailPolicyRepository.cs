using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Dapper;
using DLCS.Model.Assets;
using DLCS.Repository.Entities;
using LazyCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DLCS.Repository.Assets
{
    public class ThumbnailPolicyRepository : IThumbnailPolicyRepository
    {
        private readonly IAppCache appCache;
        private readonly IConfiguration configuration;
        private readonly ILogger<ThumbnailPolicyRepository> logger;
        private readonly IMapper mapper;

        public ThumbnailPolicyRepository(IAppCache appCache,
            IConfiguration configuration,
            ILogger<ThumbnailPolicyRepository> logger,
            IMapper mapper)
        {
            this.appCache = appCache;
            this.configuration = configuration;
            this.logger = logger;
            this.mapper = mapper;
        }
        
        public async Task<ThumbnailPolicy> GetThumbnailPolicy(string thumbnailPolicyId)
        {
            var thumbnailPolicies = await GetThumbnailPolicies();
            return thumbnailPolicies.SingleOrDefault(p => p.Id == thumbnailPolicyId);
        }

        private async Task<List<ThumbnailPolicy>> GetThumbnailPolicies()
        {
            const string key = "ThumbRepository_ThumbnailPolicies";
            return await appCache.GetOrAddAsync(key, async entry =>
            {
                // TODO - config
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                logger.LogInformation("refreshing ThumbnailPolicies from database");
                await using var connection = await DatabaseConnectionManager.GetOpenNpgSqlConnection(configuration);
                var thumbnailPolicies = await connection.QueryAsync<ThumbnailPolicyEntity>(
                    "SELECT \"Id\", \"Name\", \"Sizes\" FROM \"ThumbnailPolicies\"");
                return mapper.Map<List<ThumbnailPolicy>>(thumbnailPolicies);
            });
        }
    }
}