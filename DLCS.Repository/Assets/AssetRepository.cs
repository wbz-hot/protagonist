using System;
using System.Threading.Tasks;
using AutoMapper;
using Dapper;
using Dapper.Contrib.Extensions;
using DLCS.Core;
using DLCS.Model.Assets;
using DLCS.Repository.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DLCS.Repository.Assets
{
    public class AssetRepository : IAssetRepository
    {
        private readonly IConfiguration configuration;
        private readonly IMapper mapper;
        private readonly ILogger<AssetRepository> logger;

        public AssetRepository(IConfiguration configuration, IMapper mapper, ILogger<AssetRepository> logger)
        {
            this.configuration = configuration;
            this.mapper = mapper;
            this.logger = logger;
        }

        public async Task<Asset> GetAsset(string id)
        {
            await using var connection = await DatabaseConnectionManager.GetOpenNpgSqlConnection(configuration);
            var asset = await connection.QuerySingleOrDefaultAsync<AssetEntity>(AssetGetSql, new {Id = id});

            return mapper.Map<Asset>(asset);
        }

        public async Task<bool> UpdateAsset(Asset asset, ImageLocation imageLocation, ImageStorage imageStorage)
        {
            await using var connection = await DatabaseConnectionManager.GetOpenNpgSqlConnection(configuration);
            var transaction = await connection.BeginTransactionAsync();

            try
            {
                var assetEntity = mapper.Map<AssetEntity>(asset);

                bool success = await CallChain.ExecuteInSequence(
                    () => connection.UpdateAsync(assetEntity, transaction),
                    async () => (await connection.InsertAsync(imageLocation, transaction)) > 0,
                    async () => (await connection.InsertAsync(imageStorage, transaction)) > 0
                );

                if (success)
                {
                    await transaction.CommitAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating asset {id}", asset.Id);
            }
            
            await transaction.RollbackAsync();
            return false;
        }

        private const string AssetGetSql = @"
SELECT ""Id"", ""Customer"", ""Space"", ""Created"", ""Origin"", ""Tags"", ""Roles"", 
""PreservedUri"", ""Reference1"", ""Reference2"", ""Reference3"", ""MaxUnauthorised"", 
""NumberReference1"", ""NumberReference2"", ""NumberReference3"", ""Width"", 
""Height"", ""Error"", ""Batch"", ""Finished"", ""Ingesting"", ""ImageOptimisationPolicy"", 
""ThumbnailPolicy"", ""Family"", ""MediaType"", ""Duration""
  FROM public.""Images""
  WHERE ""Id""=@Id;";
    }
}
