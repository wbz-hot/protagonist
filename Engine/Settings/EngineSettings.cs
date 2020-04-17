using System;
using DLCS.Repository.Settings;

namespace Engine.Settings
{
    public class EngineSettings
    {
        public string ProcessingFolder { get; set; }
        
        public string ScratchRoot { get; set; }
        
        public string ImageProcessorRoot { get; set; }
        
        public ImageIngestSettings ImageIngest { get; set; }
        
        public ThumbsSettings Thumbs { get; set; }
        
        public string S3OriginRegex { get; set; }
        
        public string S3Template { get; set; }
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