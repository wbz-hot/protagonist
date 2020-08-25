using System.Threading.Tasks;

namespace Engine.Ingest.Completion
{
    public class TimebasedIngestorCompletion : IIngestorCompletion
    {
        /// <summary>
        /// Mark asset as completed in database and move assets from Transcode output to main location.
        /// </summary>
        public Task<bool> CompleteIngestion(IngestionContext context, bool ingestSuccessful, string sourceTemplate)
        {
            throw new System.NotImplementedException();
        }
    }
}