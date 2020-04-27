using System;
using System.Net.Http;
using System.Threading.Tasks;
using DLCS.Core.Guard;
using DLCS.Model.Assets;
using Microsoft.Extensions.Logging;

namespace Engine.Ingest
{
    // NOTE - this implementation makes a get request for info.json
    // this will need changed to a specific ingest endpoint
    public class OrchestratorClient
    {
        private readonly HttpClient httpClient;
        private readonly ILogger<OrchestratorClient> logger;

        public OrchestratorClient(HttpClient httpClient, ILogger<OrchestratorClient> logger)
        {
            this.httpClient = httpClient;
            this.logger = logger;
        }

        public async Task<bool> TriggerOrchestration(Asset asset)
        {
            asset.ThrowIfNull(nameof(asset));
            
            try
            {
                var path = GetOrchestrationPath(asset);
                var response = await httpClient.GetAsync(path);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error orchestrating asset {assetId} after ingestion", asset.Id);
                return false;
            }
        }
        
        private string GetOrchestrationPath(Asset asset)
            // /iiif-img/1/2/the-image/info.json
            => $"/iiif-image/{asset.Customer}/{asset.Space}/{asset.GetUniqueName()}/info.json";
    }
}