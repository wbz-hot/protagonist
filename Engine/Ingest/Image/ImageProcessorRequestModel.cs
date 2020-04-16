using System.Collections.Generic;

namespace Engine.Ingest.Image
{
    /// <summary>
    /// Request model for making requests to Tizer/Appetiser.
    /// </summary>
    public class ImageProcessorRequestModel
    {
        public string ImageId { get; set; }
        public string JobId { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }
        public string ThumbDir { get; set; }
        public IEnumerable<int> ThumbSizes { get; set; }
        public string Optimisation { get; set; }
        public string Operation { get; set; }
        public string Origin { get; set; }
    }
}