using System.Threading.Tasks;

namespace Engine.Ingest.Completion
{
    public interface IIngestorCompletion
    {
        Task<bool> CompleteIngestion(IngestionContext context, bool ingestSuccessful, string sourceTemplate);
    }
}