using DLCS.Model.Customer;
using DLCS.Repository.TypeMappings;

namespace DLCS.Repository
{
    /// <summary>
    /// Contains logic for configuring Dapper.
    /// </summary>
    public class DapperConfig
    {
        /// <summary>
        /// Initialise Dapper configuration (e.g. registers TypeHandlers or specific mappings).
        /// </summary>
        public static void Init()
        {
            Dapper.SqlMapper.AddTypeHandler(typeof(OriginStrategy), new EnumStringMapper<OriginStrategy>());
            // TODO - add AssetFamily here
        }
    }
}