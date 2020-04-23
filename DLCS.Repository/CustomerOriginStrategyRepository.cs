using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using Dapper;
using DLCS.Core.Guard;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using DLCS.Model.Security;
using DLCS.Repository.Entities;
using LazyCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DLCS.Repository
{
    public class CustomerOriginStrategyRepository : ICustomerOriginRepository
    {
        private const string OriginRegexAppSettings = "Engine:S3OriginRegex";

        private static readonly CustomerOriginStrategy DefaultStrategy = new CustomerOriginStrategy
            {Id = "_default_", Strategy = OriginStrategy.Default};

        private readonly DatabaseAccessor databaseAccessor;
        private readonly IAppCache appCache;
        private readonly ILogger<CustomerOriginStrategyRepository> logger;
        private readonly string s3OriginRegex;
        private const string CustomerOriginSql = "SELECT \"Id\", \"Customer\", \"Regex\", \"Strategy\", \"Credentials\", \"Optimised\" FROM \"CustomerOriginStrategies\"";

        public CustomerOriginStrategyRepository(
            DatabaseAccessor databaseAccessor,
            IAppCache appCache,
            IConfiguration configuration,
            ILogger<CustomerOriginStrategyRepository> logger)
        {
            this.databaseAccessor = databaseAccessor;
            this.appCache = appCache;
            this.logger = logger;
            s3OriginRegex = configuration[OriginRegexAppSettings]
                .ThrowIfNullOrWhiteSpace($"appsetting:{OriginRegexAppSettings}");
        }

        public Task<IEnumerable<CustomerOriginStrategy>> GetCustomerOriginStrategies(int customer) 
            => GetStrategiesForCustomer(customer);

        public async Task<CustomerOriginStrategy> GetCustomerOriginStrategy(Asset asset, bool initialIngestion = false)
        {
            asset.ThrowIfNull(nameof(asset));
            
            var customerStrategies = await GetCustomerOriginStrategies(asset.Customer);

            var assetOrigin = initialIngestion ? asset.GetIngestOrigin() : asset.Origin;
            var matching = FindMatchingStrategy(assetOrigin, customerStrategies) ?? DefaultStrategy;
            logger.LogTrace("Using strategy: {strategy} ('{strategyId}') for handling asset '{assetId}'", matching.Strategy, matching.Id, asset.Id);
            
            return matching;
        }

        // TODO Grab them all and store in-memory dictionary?
        private async Task<IEnumerable<CustomerOriginStrategy>> GetStrategiesForCustomer(int customer)
        {
            var key = $"CustomerOriginRepository_CustomerOrigin:{customer}";
            return await appCache.GetOrAddAsync(key, async entry =>
            {
                // TODO - config
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                logger.LogInformation("Refreshing CustomerOriginStrategy from database for customer {customer}",
                    customer);

                var origins =
                    await databaseAccessor.SelectAndMapList<CustomerOriginStrategyEntity, CustomerOriginStrategy>(
                        $"{CustomerOriginSql} WHERE \"Customer\" = @Id", new {Id = customer});
                origins.Add(GetPortalOriginStrategy(customer));
                return origins;
            });
        }

        // NOTE(DG): This CustomerOriginStrategy is for assets uploaded directly in the portal
        private CustomerOriginStrategy GetPortalOriginStrategy(int customer) 
            => new CustomerOriginStrategy
            {
                Customer = customer,
                Id = "_default_portal_",
                Regex = s3OriginRegex,
                Strategy = OriginStrategy.S3Ambient
            };

        private static CustomerOriginStrategy? FindMatchingStrategy(
            string origin,
            IEnumerable<CustomerOriginStrategy> customerStrategies)
            => customerStrategies.FirstOrDefault(cos =>
                Regex.IsMatch(origin, cos.Regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
    }
}