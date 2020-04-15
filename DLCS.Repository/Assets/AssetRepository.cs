using System.Threading.Tasks;
using AutoMapper;
using Dapper;
using DLCS.Model.Assets;
using DLCS.Repository.Entities;
using Microsoft.Extensions.Configuration;

namespace DLCS.Repository.Assets
{

    public class AssetRepository : IAssetRepository
    {
        private readonly IConfiguration configuration;
        private readonly IMapper mapper;

        public AssetRepository(IConfiguration configuration, IMapper mapper)
        {
            this.configuration = configuration;
            this.mapper = mapper;
        }

        public async Task<Asset> GetAsset(string id)
        {
            await using var connection = await DatabaseConnectionManager.GetOpenNpgSqlConnection(configuration);
            var asset = await connection.QuerySingleOrDefaultAsync<AssetEntity>(AssetSql, new {Id = id});

            return mapper.Map<Asset>(asset);
        }

        private const string AssetSql = @"
SELECT ""Id"", ""Customer"", ""Space"", ""Created"", ""Origin"", ""Tags"", ""Roles"", 
""PreservedUri"", ""Reference1"", ""Reference2"", ""Reference3"", ""MaxUnauthorised"", 
""NumberReference1"", ""NumberReference2"", ""NumberReference3"", ""Width"", 
""Height"", ""Error"", ""Batch"", ""Finished"", ""Ingesting"", ""ImageOptimisationPolicy"", 
""ThumbnailPolicy"", ""Family"", ""MediaType"", ""Duration""
  FROM public.""Images""
  WHERE ""Id""=@Id;";
    }
}
