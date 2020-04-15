using System.Threading.Tasks;
using Engine.Ingest.Models;
using Engine.Settings;
using Microsoft.Extensions.Options;

namespace Engine.Ingest.Workers
{
    public class TimebasedIngesterWorker : AssetIngesterWorker
    {
        public TimebasedIngesterWorker(IAssetFetcher assetFetcher, IOptionsMonitor<EngineSettings> optionsMonitor) 
            : base(assetFetcher, optionsMonitor)
        {
        }
        
        protected override Task FamilySpecificIngest(IngestAssetRequest thingToIngestAsset)
        {
            return Task.CompletedTask;
        }
    }
}