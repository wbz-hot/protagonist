namespace Engine.Settings
{
    public class CustomerOverridesSettings
    {
        public static CustomerOverridesSettings Empty = new CustomerOverridesSettings {CustomerName = "_default_"};
        
        /// <summary>
        /// Friendly name of customer (keyed by Id in appsettings).
        /// </summary>
        public string CustomerName { get; set; }
        
        /// <summary>
        /// Whether image should immediately be orchestrated after ingestion.
        /// </summary>
        public bool? OrchestrateImageAfterIngest { get; set; }
        
        /// <summary>
        /// If true, StoragePolicy is not checked on ingestion.
        /// </summary>
        public bool NoStoragePolicyCheck { get; set; }
    }
}