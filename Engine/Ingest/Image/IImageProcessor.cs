using System.Threading.Tasks;

namespace Engine.Ingest.Image
{
    public interface IImageProcessor
    {
        Task<bool> ProcessImage(IngestionContext context);
    }
}