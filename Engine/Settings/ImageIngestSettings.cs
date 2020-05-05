using System;

namespace Engine.Settings
{
    /// <summary>
    /// Settings directly related to image ingestion
    /// </summary>
    /// <remarks>These will be Tizer/Appetiser settings.</remarks>
    public class ImageIngestSettings
    {
        public string SourceTemplate { get; set; }
        
        public string DestinationTemplate { get; set; }
        
        public string ThumbsTemplate { get; set; }
        
        public string S3Template { get; set; }
        
        public Uri ImageProcessorUrl { get; set; }

        public int ImageProcessorTimeoutMs { get; set; } = 300000;
    }
}