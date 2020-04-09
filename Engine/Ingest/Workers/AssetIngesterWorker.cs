using System.Threading;
using System.Threading.Tasks;
using Engine.Ingest.Models;

namespace Engine.Ingest.Workers
{
    /// <summary>
    /// Base class for ingestions.
    /// </summary>
    public abstract class AssetIngesterWorker
    {
        public async Task<IngestResult> Ingest(IngestAssetRequest ingestAssetRequest,
            CancellationToken cancellationToken)
        {
            // load data

            // call image or ElasticTranscoder
            await FamilySpecificIngest(ingestAssetRequest);
            
            // update batch
            // set response (if image)

            return IngestResult.Success;
        }

        // TODO - what needs pushed to this method?
        protected abstract Task FamilySpecificIngest(IngestAssetRequest thingToIngestAsset);
    }
}