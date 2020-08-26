using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using DLCS.Core;
using DLCS.Model.Assets;
using DLCS.Repository.Entities;
using Microsoft.Extensions.Logging;

namespace DLCS.Repository.Assets
{
    public class AssetRepository : IAssetRepository
    {
        private readonly ILogger<AssetRepository> logger;
        private readonly DatabaseAccessor dbFactory;

        public AssetRepository(DatabaseAccessor dbFactory, ILogger<AssetRepository> logger)
        {
            this.dbFactory = dbFactory;
            this.logger = logger;
        }

        public Task<Asset> GetAsset(string id)
            => dbFactory.SelectAndMap<AssetEntity, Asset>(AssetGetSql, new {Id = id});

        // TODO - should this live in Engine only?
        public async Task<bool> UpdateIngestedAsset(Asset asset, ImageLocation? imageLocation, ImageStorage imageStorage)
        {
            logger.LogDebug("Marking asset {assetId} as completed", asset.Id);
            asset.MarkAsIngestComplete();
            
            await using var connection = await dbFactory.GetOpenDbConnection();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var success = await CallChain.ExecuteInSequence(
                    () => dbFactory.MapAndExecute<Asset, AssetEntity>(asset, AssetUpdateSql, transaction),
                    () => dbFactory.Execute(imageLocation, ImageLocationUpdateSql, transaction),
                    () => dbFactory.Execute(imageStorage, ImageStorageUpsertSql, transaction),
                    () => UpdateBatch(asset, transaction));

                if (success)
                {
                    await transaction.CommitAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating asset {assetId}", asset.Id);
            }
            
            await transaction.RollbackAsync();
            return false;
        }

        // TODO - move this to own repo?
        private async Task<bool> UpdateBatch(Asset asset, IDbTransaction transaction)
        {
            // Batch is non nullable, 0 == no batch
            if (asset.Batch == 0) return true;
            
            // TODO - this should be DateTime.UtcNow
            var param = new {Id = asset.Batch, Now = DateTime.Now};
            var result = asset.HasError
                ? await transaction.Connection.ExecuteAsync(IncrementBatchErrorsSql, param, transaction)
                : await transaction.Connection.ExecuteAsync(IncrementBatchCompletedSql, param, transaction);

            return result > 0;
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
            @"UPDATE ""Images"" SET ""Width"" = @Width, ""Height"" = @Height, ""Error"" = @Error, ""Finished"" = @Finished, ""Ingesting"" = @Ingesting, ""Duration"" = @Duration WHERE ""Id"" = @Id;";

        private const string ImageLocationInsertSql =
            @"INSERT INTO ""ImageLocation"" (""Id"", ""S3"", ""Nas"") VALUES (@Id, @S3, @Nas);";
        
        private const string ImageLocationUpdateSql =
            @"UPDATE ""ImageLocation"" SET ""S3"" = @S3, ""Nas""= @Nas WHERE ""Id"" = @Id;";
        
        private const string ImageStorageUpsertSql =
            @"
INSERT INTO ""ImageStorage"" (""Id"", ""Customer"", ""Space"", ""ThumbnailSize"", ""Size"", ""LastChecked"", ""CheckingInProgress"")
VALUES (@Id, @Customer, @Space, @ThumbnailSize, @Size, @LastChecked, @CheckingInProgress)
ON CONFLICT (""Id"", ""Customer"", ""Space"") DO UPDATE
  SET ""Customer""=excluded.""Customer"", ""Space""=excluded.""Space"", ""ThumbnailSize""=excluded.""ThumbnailSize"",
      ""Size""=excluded.""Size"", ""LastChecked""=excluded.""LastChecked"", ""CheckingInProgress""=excluded.""CheckingInProgress"";
";
        
        private const string IncrementBatchErrorsSql = @"
UPDATE ""Batches"" SET ""Errors""=""Errors""+1 WHERE ""Id"" = @Id;
UPDATE ""Batches"" SET ""Finished""=@Now WHERE ""Id"" = @Id AND ""Completed""+""Errors""=""Count""";
        
        private const string IncrementBatchCompletedSql = @"
UPDATE ""Batches"" SET ""Completed""=""Completed""+1 WHERE ""Id"" = @Id;
UPDATE ""Batches"" SET ""Finished""=@Now WHERE ""Id"" = @Id AND ""Completed""+""Errors""=""Count""";
    }
}
