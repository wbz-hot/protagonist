using System.Threading;
using System.Threading.Tasks;
using Engine.Ingest.Models;

namespace Engine.Ingest.Workers
{
    public class TimebasedIngesterWorker : IAssetIngesterWorker
    {
        public Task<IngestResult> Ingest(IngestAssetRequest ingestAssetRequest, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}