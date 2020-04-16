using System.Threading.Tasks;
using Engine.Ingest.Models;
using Engine.Settings;
using Microsoft.Extensions.Options;

namespace Engine.Ingest.Workers
{
    public class ImageIngesterWorker : AssetIngesterWorker
    {
        public ImageIngesterWorker(IAssetFetcher assetFetcher, IOptionsMonitor<EngineSettings> optionsMonitor) 
            : base(assetFetcher, optionsMonitor)
        {
        }

        protected override Task FamilySpecificIngest(IngestionContext thingToIngestAsset)
        {
            /* TODO
             - move files around
             - get thumbnailPolicy
             - get imageOptimisationPolicy
             - call tizer
             - handle tizer response
             */
            
            
            return Task.CompletedTask;
        }
    }
}