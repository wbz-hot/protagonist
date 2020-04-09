using System.Threading.Tasks;
using Engine.Ingest.Models;

namespace Engine.Ingest.Workers
{
    public class TimebasedIngesterWorker : AssetIngesterWorker
    {
        protected override Task FamilySpecificIngest(IngestAssetRequest thingToIngestAsset)
        {
            return Task.CompletedTask;
        }
    }
}