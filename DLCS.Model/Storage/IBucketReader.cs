using System.IO;
using System.Threading.Tasks;

namespace DLCS.Model.Storage
{
    /// <summary>
    /// Interface wrapping interactions with cloud blob storage.
    /// </summary>
    public interface IBucketReader
    {
        string DefaultRegion { get; }
        
        /// <summary>
        /// Get specified object from bucket.
        /// </summary>
        /// <param name="objectInBucket">Object to read.</param>
        Task<Stream?> GetObjectContentFromBucket(ObjectInBucket objectInBucket);

        /// <summary>
        /// Get full object from bucket, including content and headers.
        /// </summary>
        Task<ObjectFromBucket> GetObjectFromBucket(ObjectInBucket objectInBucket);
        
        Task<string[]> GetMatchingKeys(ObjectInBucket rootKey);
        
        Task<bool> CopyWithinBucket(string bucket, string sourceKey, string destKey);
        
        Task<bool> WriteToBucket(ObjectInBucket dest, string content, string contentType);
        
        /// <summary>
        /// Write file from disk to S3 bucket.
        /// </summary>
        /// <param name="dest">Target object to write.</param>
        /// <param name="filePath">File on disk to write to S3.</param>
        Task<bool> WriteFileToBucket(ObjectInBucket dest, string filePath);

        /// <summary>
        /// Delete specified objects underlying storage.
        /// NOTE: This method assumes all objects are in the same bucket.
        /// </summary>
        /// <param name="toDelete">List of objects to delete</param>
        Task DeleteFromBucket(params ObjectInBucket[] toDelete);
    }
}
