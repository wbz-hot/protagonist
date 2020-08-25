using System;
using System.IO;
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
            engineSettings = engineOptions.CurrentValue;
        }

        /// <summary>
        /// Mark asset as completed in database, clean up working assets and optionally trigger orchestration.
        /// </summary>
        public async Task<bool> CompleteIngestion(IngestionContext context, bool ingestSuccessful,
            string sourceTemplate)
        {
            var success = await MarkAssetAsIngested(context);

            if (ingestSuccessful && success)
            {
                await TriggerOrchestration(context);
            }
            
            // Processing has occurred, clear down the root folder used for processing
            CleanupWorkingAssets(sourceTemplate);

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
                logger.LogError(e, "Error marking image as completed '{assetId}'", context.Asset.Id);
                return false;
            }
        }

        private async Task TriggerOrchestration(IngestionContext context)
        {
            if (!ShouldOrchestrate(context.Asset.Customer)) return;

            var orchestrationSuccess = await orchestratorClient.TriggerOrchestration(context.Asset);
            if (!orchestrationSuccess)
            {
                logger.LogInformation("Attempt to orchestrate '{assetId}' failed", context.Asset.Id);
            }
        }
        
        private bool ShouldOrchestrate(int customerId)
        {
            var customerSpecific = engineSettings.GetCustomerSettings(customerId);
            return customerSpecific.OrchestrateImageAfterIngest ?? engineSettings.OrchestrateImageAfterIngest;
        }
        
        private void CleanupWorkingAssets(string rootPath)
        {
            try
            {
                Directory.Delete(rootPath, true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error cleaning up working assets from '{rootPath}'", rootPath);
            }
        }
    }
}