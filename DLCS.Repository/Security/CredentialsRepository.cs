﻿using System;
using System.IO;
using System.Threading.Tasks;
using DLCS.Core.Guard;
using DLCS.Model.Customer;
using DLCS.Model.Security;
using DLCS.Model.Storage;
using LazyCache;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DLCS.Repository.Security
{
    public class CredentialsRepository : ICredentialsRepository
    {
        private readonly IBucketReader bucketReader;
        private readonly IAppCache appCache;
        private readonly ILogger<CredentialsRepository> logger;

        public CredentialsRepository(IBucketReader bucketReader,
            IAppCache appCache,
            ILogger<CredentialsRepository> logger)
        {
            this.bucketReader = bucketReader;
            this.appCache = appCache;
            this.logger = logger;
        }
        
        public Task<BasicCredentials?> GetBasicCredentialsForOriginStrategy(CustomerOriginStrategy customerOriginStrategy)
        {
            customerOriginStrategy.ThrowIfNull(nameof(customerOriginStrategy));
            var credentials =
                customerOriginStrategy.Credentials.ThrowIfNullOrWhiteSpace(nameof(customerOriginStrategy.Credentials));

            var cacheKey = $"OriginStrategy_Creds:{customerOriginStrategy.Id}";

            return appCache.GetOrAddAsync(cacheKey, entry =>
            {
                // TODO - config
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                logger.LogDebug("Refreshing CustomerOriginStrategy credentials for {customerOriginStrategy}",
                    customerOriginStrategy.Id);
                return GetBasicCredentials(credentials, customerOriginStrategy.Id);
            });
        }
        
        private async Task<BasicCredentials?> GetBasicCredentials(string credentials, string id)
        {
            try
            {
                if (CredentialsAreForS3(credentials))
                {
                    // get from s3
                    return await GetBasicCredentialsFromBucket(credentials);
                }

                // deserialize from object in DB
                var basicCredentials = JsonConvert.DeserializeObject<BasicCredentials>(credentials);
                return basicCredentials;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting credentials for customerOriginStrategy {customerOriginStrategy}", id);
                throw;
            }
        }
        
        private static bool CredentialsAreForS3(string credentials) => credentials.StartsWith("s3://");

        private async Task<BasicCredentials> GetBasicCredentialsFromBucket(string credentials)
        {
            var objectInBucket = RegionalisedObjectInBucket.Parse(credentials);
            var credentialStream = await bucketReader.GetObjectContentFromBucket(objectInBucket);
            using var reader = new StreamReader(credentialStream);
            using var jsonReader = new JsonTextReader(reader);
            return new JsonSerializer().Deserialize<BasicCredentials>(jsonReader);
        }
    }
}