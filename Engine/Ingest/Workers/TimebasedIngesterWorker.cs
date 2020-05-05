using System;
using System.Threading;
using System.Threading.Tasks;
using Engine.Ingest.Models;
using Engine.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Engine.Ingest.Workers
{
    public class TimebasedIngesterWorker : IAssetIngesterWorker
    {
        private readonly IAssetFetcher assetFetcher;
        private readonly EngineSettings engineSettings;
        private readonly ILogger<TimebasedIngesterWorker> logger;

        public TimebasedIngesterWorker(
            IAssetFetcher assetFetcher,
            IOptionsMonitor<EngineSettings> engineOptions,
            ILogger<TimebasedIngesterWorker> logger)
        {
            this.assetFetcher = assetFetcher;
            engineSettings = engineOptions.CurrentValue;
            this.logger = logger;
        }

        public async Task<IngestResult> Ingest(IngestAssetRequest ingestAssetRequest, CancellationToken cancellationToken)
        {
            /*
             * fire fetch-from-origin if we aren't a full-bucket-access audio/video item (and source is s3)
             * then copy from local to s3
             *
             * if full-bucket-access then do an s3-s3 copy
             *
             * then create a new ElasticTranscoder job
             */

            try
            {
                var customerOverride = GetCustomerOverrideSettings(ingestAssetRequest.Asset.Customer);

                // need to know customerOriginStrategy here?
                if (customerOverride.FullBucketAccess)
                {
                    
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error ingesting image {assetId}", ingestAssetRequest.Asset.Id);
                return IngestResult.Failed;
            }

            throw new System.NotImplementedException();
        }

        private CustomerOverridesSettings GetCustomerOverrideSettings(int customerId)
            => engineSettings.GetCustomerSettings(customerId);
    }
}