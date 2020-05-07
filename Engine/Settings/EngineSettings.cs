using System;
using System.Collections.Generic;
using DLCS.Repository.Settings;

namespace Engine.Settings
{
    public class EngineSettings
    {
        public ImageIngestSettings ImageIngest { get; set; }
        
        public TimebasedIngestSettings TimebasedIngest { get; set; }
        
        public ThumbsSettings Thumbs { get; set; }
        
        public string S3OriginRegex { get; set; }

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