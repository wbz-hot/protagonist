using System;
using System.Threading.Tasks;
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

        public async Task<bool> UpdateIngestedAsset(Asset asset, ImageLocation imageLocation, ImageStorage imageStorage)
        {
            await using var connection = await dbFactory.GetOpenDbConnection();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var success = await CallChain.ExecuteInSequence(
                    () => dbFactory.MapAndExecute<Asset, AssetEntity>(asset, AssetUpdateSql, transaction),
                    () => dbFactory.Execute(imageLocation, ImageLocationUpdateSql, transaction),
                    () => dbFactory.Execute(imageStorage, ImageStorageInsertSql, transaction));

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

        private const string AssetUpdateSql =
            @"UPDATE ""Images"" SET ""Width"" = @Width, ""Height"" = @Height, ""Error"" = @Error, ""Finished"" = @Finished, ""Ingesting"" = @Ingesting WHERE ""Id"" = @Id;";

        private const string ImageLocationInsertSql =
            @"INSERT INTO ""ImageLocation"" (""Id"", ""S3"", ""Nas"") VALUES (@Id, @S3, @Nas);";
        
        private const string ImageLocationUpdateSql =
            @"UPDATE ""ImageLocation"" SET ""S3"" = @S3, ""Nas""= @Nas WHERE ""Id"" = @Id;";
        
        private const string ImageStorageInsertSql =
            @"
INSERT INTO ""ImageStorage"" (""Id"", ""Customer"", ""Space"", ""ThumbnailSize"", ""Size"", ""LastChecked"", ""CheckingInProgress"") 
VALUES (@Id, @Customer, @Space, @ThumbnailSize, @Size, @LastChecked, @CheckingInProgress);";
    }
}
