using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Engine.Ingest.Timebased;

namespace Engine.Ingest.Completion
{
    public interface ITimebasedIngestorCompletion
    {
        /// <summary>
        /// Mark asset as completed in database and move assets from Transcode output to main location.
        /// </summary>
        Task<bool> CompleteIngestion(string assetId, IList<TranscodeOutput> transcodeOutputs,
            CancellationToken cancellationToken);
    }
}