using System;
using System.Collections.Generic;
using DLCS.Repository.Settings;

namespace Engine.Settings
{
    public class EngineSettings
    {
        public string ProcessingFolder { get; set; }
        
        /// <summary>
        /// Root folder for main container
        /// </summary>
        public string ScratchRoot { get; set; }
        
        /// <summary>
        /// Root folder for use by Image-Processor sidecar
        /// </summary>
        public string ImageProcessorRoot { get; set; }
        
        public ImageIngestSettings ImageIngest { get; set; }
        
        public ThumbsSettings Thumbs { get; set; }
        
        public string S3OriginRegex { get; set; }
        
        public string S3Template { get; set; }
        
        /// <summary>
        /// Whether image should immediately be orchestrated after ingestion.
        /// </summary>
        public string OrchestrateImageAfterIngest { get; set; }
        
        public Dictionary<int, CustomerOverridesSettings> CustomerOverrides { get; set; }

        /// <summary>
        /// Get the root folder, if forImageProcessor will ensure that it is compatible with needs of image-processor
        /// sidecar.
        /// </summary>
        public string GetRoot(bool forImageProcessor = false)
        {
            if (!forImageProcessor) return ScratchRoot;

            return string.IsNullOrEmpty(ImageProcessorRoot)
                ? ScratchRoot
                : ImageProcessorRoot;
        }
    }

    public class CustomerOverridesSettings
    {
        /// <summary>
        /// 
        /// </summary>
        public string CustomerName { get; set; }
        
        /// <summary>
        /// Whether image should immediately be orchestrated after ingestion.
        /// </summary>
        public string OrchestrateImageAfterIngest { get; set; }
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