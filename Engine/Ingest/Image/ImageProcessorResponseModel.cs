using System.Collections.Generic;

namespace Engine.Ingest.Image
{
    /// <summary>
    /// Response model for receiving requests back from Tizer/Appetiser.
    /// </summary>
    public class ImageProcessorResponseModel
    {
        public string ImageId { get; set; }
        public string JobId { get; set; }
        public string Optimisation { get; set; }
        public string JP2 { get; set; }
        public string Origin { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public string InfoJson { get; set; }
        public IEnumerable<ImageProcessorThumb> Thumbs { get; set; }
    }
}