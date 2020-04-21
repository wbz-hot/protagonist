using System.Threading.Tasks;
using Engine.Ingest.Models;
using Engine.Settings;
using Microsoft.Extensions.Options;

namespace Engine.Ingest.Workers
{
    public class TimebasedIngesterWorker : AssetIngesterWorker
    {
        public TimebasedIngesterWorker(IAssetFetcher assetFetcher, IOptionsMonitor<EngineSettings> engineOptionsMonitor) 
            : base(assetFetcher, engineOptionsMonitor)
        {
        }
        
        protected override Task<IngestResult> FamilySpecificIngest(IngestionContext ingestionContext)
        {
            return Task.FromResult(IngestResult.Unknown);
        }
    }
}