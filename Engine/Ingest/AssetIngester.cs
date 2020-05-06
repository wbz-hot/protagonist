using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using DLCS.Model.Policies;
using Engine.Ingest.Models;
using Engine.Ingest.Strategy;
using Engine.Ingest.Workers;
using Microsoft.Extensions.Logging;

namespace Engine.Ingest
{
    /// <summary>
    /// Delegate for getting the correct <see cref="IAssetIngesterWorker"/> for specified Family.
    /// </summary>
    /// <param name="family">The type of ingester worker.</param>
    public delegate IAssetIngesterWorker IngestorResolver(AssetFamily family);

    /// <summary>
    /// Contains operations for ingesting assets.
    /// </summary>
    public class AssetIngester
    {
        private readonly IngestorResolver resolver;
        private readonly ILogger<AssetIngester> logger;
        private readonly ICustomerOriginRepository customerOriginRepository;
        private readonly IPolicyRepository policyRepository;

        public AssetIngester(
            IngestorResolver resolver, 
            ILogger<AssetIngester> logger,
            ICustomerOriginRepository customerOriginRepository,
            IPolicyRepository policyRepository)
        {
            this.resolver = resolver;
            this.logger = logger;
            this.customerOriginRepository = customerOriginRepository;
            this.policyRepository = policyRepository;
        }
        
        /// <summary>
        /// Run ingest based on <see cref="IncomingIngestEvent"/>.
        /// </summary>
        /// <returns>Result of ingest operations</returns>
        /// <remarks>This is to comply with message format sent by Deliverator API.</remarks>
        public Task<IngestResult> Ingest(IncomingIngestEvent request, CancellationToken cancellationToken)
        {
            try
            {
                var internalIngestRequest = request.ConvertToInternalRequest();
                return Ingest(internalIngestRequest, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception ingesting IncomingIngest", request.Message);
                return Task.FromResult(IngestResult.Failed);
            }
        }

        /// <summary>
        /// Run ingest based on <see cref="IngestAssetRequest"/>.
        /// </summary>
        /// <returns>Result of ingest operations</returns>
        public async Task<IngestResult> Ingest(IngestAssetRequest request, CancellationToken cancellationToken)
        {
            // TODO - the true param here may be false if reingesting??
            var getCustomerOriginStrategy = customerOriginRepository.GetCustomerOriginStrategy(request.Asset, true);
            
            // set Thumbnail and ImageOptimisation policies
            var setAssetPolicies = policyRepository.HydrateAssetPolicies(request.Asset, AssetPolicies.All);
            
            var ingestor = resolver(request.Asset.Family);

            await Task.WhenAll(getCustomerOriginStrategy, setAssetPolicies);

            return await ingestor.Ingest(request, getCustomerOriginStrategy.Result, cancellationToken);
        }
    }
}