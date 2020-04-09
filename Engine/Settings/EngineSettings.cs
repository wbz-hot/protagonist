using System;

namespace Engine.Settings
{
    public class EngineSettings
    {
        public string ProcessingFolder { get; set; }
        
        public string ScratchRoot { get; set; }
        
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
        
        public string ThumbsFolder { get; set; }
        
        public Uri ImageProcessorUrl { get; set; }
    }
}