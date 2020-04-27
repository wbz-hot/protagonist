using System;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using Engine.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Engine.Ingest.Completion
{
    public class ImageIngestorCompletion : IIngestorCompletion
    {
        private readonly OrchestratorClient orchestratorClient;
        private readonly IAssetRepository assetRepository;
        private readonly ILogger<ImageIngestorCompletion> logger;
        private readonly EngineSettings engineSettings;

        public ImageIngestorCompletion(
            OrchestratorClient orchestratorClient,
            IAssetRepository assetRepository,
            IOptionsMonitor<EngineSettings> engineOptions,
            ILogger<ImageIngestorCompletion> logger)
        {
            this.orchestratorClient = orchestratorClient;
            this.assetRepository = assetRepository;
            this.logger = logger;
            this.engineSettings = engineOptions.CurrentValue;
        }

        public async Task<bool> CompleteIngestion(IngestionContext context, bool ingestSuccessful)
        {
            // TODO - can this be used for Timebased too?
            var success = await MarkAssetAsIngested(context);

            if (ingestSuccessful && success)
            {
                await TriggerOrchestration(context);
            }

            return success;
        }

        private async Task<bool> MarkAssetAsIngested(IngestionContext context)
        {
            try
            {
                var success =
                    await assetRepository.UpdateIngestedAsset(context.Asset, context.ImageLocation, context.ImageStorage);
                return success;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error updating image {asset}", context.Asset.Id);
                return false;
            }
        }

        private async Task TriggerOrchestration(IngestionContext context)
        {
            if (!ShouldIngest(context.Asset.Customer)) return;

            var orchestrationSuccess = await orchestratorClient.TriggerOrchestration(context.Asset);
            if (!orchestrationSuccess)
            {
                logger.LogInformation("Attempt to orchestrate '{assetId}' failed", context.Asset.Id);
            }
        }
        
        private bool ShouldIngest(int customerId)
        {
            var customerSpecific = engineSettings.GetCustomerSettings(customerId);
            return customerSpecific.OrchestrateImageAfterIngest ?? engineSettings.OrchestrateImageAfterIngest;
        }
    }
}