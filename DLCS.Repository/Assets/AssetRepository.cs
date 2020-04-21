using System;
using System.Data;
using System.Threading.Tasks;
using AutoMapper;
using Dapper;
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

        public async Task<bool> UpdateIngestedAsset(Asset asset, ImageLocation imageLocation, ImageStorage imageStorage)
        {
            await using var connection = await DatabaseConnectionManager.GetOpenNpgSqlConnection(configuration);
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                bool success = await CallChain.ExecuteInSequence(
                    () => UpdateAsset(asset, transaction),
                    () => Insert(imageLocation, ImageLocationInsertSql, transaction),
                    () => Insert(imageStorage, ImageStorageInsertSql, transaction));

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
        
        private async Task<bool> UpdateAsset(Asset asset, IDbTransaction transaction)
        {
            try
            {
                var assetEntity = mapper.Map<AssetEntity>(asset);
                await transaction.Connection.ExecuteAsync(AssetUpdateSql, assetEntity, transaction);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating asset {id}", asset.Id);
                return false;
            }
        }

        // TODO - move this to a base method? or a helper class?
        private async Task<bool> Insert<T>(T type, string sql, IDbTransaction transaction)
            where T : class
        {
            if (type == null) return true;
            try
            {
                await transaction.Connection.ExecuteAsync(sql, type, transaction);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error inserting item of type {type}.", typeof(T).Name);
                return false;
            }
        }

        private const string AssetGetSql = @"
SELECT ""Id"", ""Customer"", ""Space"", ""Created"", ""Origin"", ""Tags"", ""Roles"", 
""PreservedUri"", ""Reference1"", ""Reference2"", ""Reference3"", ""MaxUnauthorised"", 
""NumberReference1"", ""NumberReference2"", ""NumberReference3"", ""Width"", 
""Height"", ""Error"", ""Batch"", ""Finished"", ""Ingesting"", ""ImageOptimisationPolicy"", 
""ThumbnailPolicy"", ""Family"", ""MediaType"", ""Duration""
  FROM public.""Images""
  WHERE ""Id""=@Id;";

        private const string AssetUpdateSql =
            @"UPDATE ""Images"" SET ""Width"" = @Width, ""Height"" = @Height, ""Error"" = @Error WHERE ""Id"" = @Id;";

        private const string ImageLocationInsertSql =
            @"INSERT INTO ""ImageLocation"" (""Id"", ""S3"", ""Nas"") VALUES (@Id, @S3, @Nas)";
        
        private const string ImageStorageInsertSql =
            @"
INSERT INTO ""ImageStorage"" (""Id"", ""Customer"", ""Space"", ""ThumbnailSize"", ""Size"", ""LastChecked"", ""CheckingInProgress"") 
VALUES (@Id, @Customer, @Space, @ThumbnailSize, @Size, @LastChecked, @CheckingInProgress)";
    }
}
