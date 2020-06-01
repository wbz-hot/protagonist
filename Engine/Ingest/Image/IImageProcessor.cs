using System.Threading.Tasks;

namespace Engine.Ingest.Image
{
    /// <summary>
    /// Interacts with downstream image-processor and transfers generated files. 
    /// </summary>
    public interface IImageProcessor
    {
        /// <summary>
        /// Make request to image-processor to generate thumbnails and/or tile-optimised image file.
        /// Copy generated files to destination 'slow' storage, if appropriate. 
        /// </summary>
        /// <param name="context">Object representing current ingestion operation.</param>
        /// <returns>true if succeeded, else false.</returns>
        Task<bool> ProcessImage(IngestionContext context);
    }
}