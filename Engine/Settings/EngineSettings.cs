using System;
using System.Collections.Generic;
using DLCS.Repository.Settings;

namespace Engine.Settings
{
    public class EngineSettings
    {
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
        public bool OrchestrateImageAfterIngest { get; set; }
        
        /// <summary>
        /// A collection of customer-specific overrides, keyed by customerId.
        /// </summary> 
        public Dictionary<string, CustomerOverridesSettings> CustomerOverrides { get; set; } =
            new Dictionary<string, CustomerOverridesSettings>();
        
        /// <summary>
        /// Base url for calling orchestrator.
        /// </summary>
        public Uri OrchestratorBaseUrl { get; set; }
        
        public int OrchestratorTimeoutMs { get; set; } = 5000;

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

        /// <summary>
        /// Get CustomerSpecificSettings, if found. 
        /// </summary>
        /// <param name="customerId">CustomerId to get settings for.</param>
        /// <returns>Customer specific overrides, or default if not found.</returns>
        public CustomerOverridesSettings GetCustomerSettings(int customerId)
            => CustomerOverrides.TryGetValue(customerId.ToString(), out var settings)
                ? settings
                : CustomerOverridesSettings.Empty;
    }
}