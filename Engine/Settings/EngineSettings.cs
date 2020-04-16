using System;

namespace Engine.Settings
{
    public class EngineSettings
    {
        public string ProcessingFolder { get; set; }
        
        public string ScratchRoot { get; set; }
        
        public string ImageProcessorRoot { get; set; }
        
        public ImageIngestSettings ImageIngest { get; set; }
    }

    /// <summary>
    /// Settings directly related to image ingestion
    /// </summary>
    /// <remarks>These will be Tizer/Appetiser settings.</remarks>
    public class ImageIngestSettings
    {
        public string SourceTemplate { get; set; }
        
        public string DestinationTemplate { get; set; }
        
        public string ThumbsTemplate { get; set; }
        
        public Uri ImageProcessorUrl { get; set; }

        public int ImageProcessorTimeoutMs { get; set; } = 300000;
    }
}