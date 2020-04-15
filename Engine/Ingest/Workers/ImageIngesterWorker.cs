using System.Threading.Tasks;
using Engine.Ingest.Models;

namespace Engine.Ingest.Workers
{
    public class ImageIngesterWorker : AssetIngesterWorker
    {
        public ImageIngesterWorker(IAssetFetcher assetFetcher) : base(assetFetcher)
        {
        }
        
        protected override Task FamilySpecificIngest(IngestAssetRequest thingToIngestAsset)
        {
            return Task.CompletedTask;
        }
    }
}