using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using DLCS.Core.Guard;
using DLCS.Model.Customer;
using LazyCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DLCS.Repository
{
    public class CustomerOriginStrategyRepository : ICustomerOriginRepository
    {
        private const string OriginRegexAppSettings = "s3OriginRegex";
        
        private readonly IAppCache appCache;
        private readonly IConfiguration configuration;
        private readonly ILogger<CustomerOriginStrategyRepository> logger;
        private readonly string originRegexAppSetting;
        private const string CustomerOriginSql = "SELECT \"Id\", \"Customer\", \"Regex\", \"Strategy\", \"Credentials\", \"Optimised\" FROM public.\"CustomerOriginStrategy\"";

        public CustomerOriginStrategyRepository(
            IAppCache appCache,
            IConfiguration configuration,
            ILogger<CustomerOriginStrategyRepository> logger)
        {
            this.appCache = appCache;
            this.configuration = configuration;
            this.logger = logger;
            originRegexAppSetting = configuration[OriginRegexAppSettings].ThrowIfNullOrWhiteSpace($"appsetting:{OriginRegexAppSettings}");
        }

        public Task<IEnumerable<CustomerOriginStrategy>> GetCustomerOriginStrategy(int customer) 
            => GetStrategiesForCustomer(customer);

        // TODO Grab them all and store in-memory dictionary?
        private async Task<IEnumerable<CustomerOriginStrategy>> GetStrategiesForCustomer(int customer)
        {
            var key = $"CustomerOriginRepository_CustomerOrigin:{customer}";
            return await appCache.GetOrAddAsync(key, async entry =>
            {
                // TODO - config
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                logger.LogInformation("Refreshing CustomerOriginStrategy from database for customer {customer}", customer);
                await using var connection = await DatabaseConnectionManager.GetOpenNpgSqlConnection(configuration);
                var customerOriginStrategies = await connection.QueryAsync<CustomerOriginStrategy>(
                    $"{CustomerOriginSql} WHERE \"Customer\" = @Id", new {Id = customer});
                
                var allOrigins = customerOriginStrategies.ToList();
                allOrigins.Add(GetPortalOriginStrategy(customer));
                return allOrigins;
            });
        }

        // NOTE(DG): This CustomerOriginStrategy is for assets uploaded directly in the portal
        private CustomerOriginStrategy GetPortalOriginStrategy(int customer) 
            => new CustomerOriginStrategy
            {
                Customer = customer,
                Id = "_default_portal_",
                Regex = originRegexAppSetting,
                Strategy = OriginStrategy.S3Ambient
            };
    }
}