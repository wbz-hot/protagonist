using System.Threading;
using System.Threading.Tasks;
using Engine.Ingest.Models;

namespace Engine.Ingest.Workers
{
    /// <summary>
    /// Interface for operations related to ingesting assets.
    /// </summary>
    public interface IAssetIngesterWorker
    {
        /// <summary>
        /// Ingest provided asset
        /// </summary>
        Task<IngestResult> Ingest(IngestAssetRequest ingestAssetRequest,
            CancellationToken cancellationToken);
    }

    public interface IAssetIngesterCompleted
    {
        Task<IngestResult> IngestComplete(IngestionContext ingestionContext);
    }
}