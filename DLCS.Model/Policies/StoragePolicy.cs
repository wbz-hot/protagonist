namespace DLCS.Model.Policies
{
    /// <summary>
    /// Represents an entity from the "StoragePolicies" table.
    /// </summary>
    public class StoragePolicy
    {
        /// <summary>
        /// Unique identifier for storage policy.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The maximum number of individual source images that can be stored.
        /// </summary>
        public int MaximumNumberOfStoredImages { get; set; }
        
        /// <summary>
        /// The maximum total size (in bytes) of storage images, excluding thumbs.
        /// </summary>
        public int MaximumTotalSizeOfStoredImages { get; set; }
    }
}