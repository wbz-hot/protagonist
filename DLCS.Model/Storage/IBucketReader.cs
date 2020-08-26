using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Core;

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

        /// <summary>
        /// Copy large file from disk to bucket.
        /// </summary>
        /// <param name="dest">Target object to write.</param>
        /// <param name="filePath">File on disk to write to S3.</param>
        /// <param name="contentType">Optional content type</param>
        /// <param name="token">Cancellation token</param>
        Task<bool> WriteLargeFileToBucket(ObjectInBucket dest, string filePath, string? contentType = null,
            CancellationToken token = default);

        /// <summary>
        /// Copy large file from one bucket to another.
        /// </summary>
        /// <param name="source">Source item to copy.</param>
        /// <param name="target">Where to copy item to.</param>
        /// <param name="verifySize">Function to verify objectSize prior to copying. Not copied if false returned.</param>
        /// <param name="targetIsOpen">If true the copied object is given public access rights</param>
        /// <param name="token">Cancellation token</param>
        /// <returns><see>
        ///         <cref>ResultStatus{long?}</cref>
        ///     </see>
        ///     representing success of call and file size</returns>
        Task<ResultStatus<long?>> CopyLargeFileBetweenBuckets(ObjectInBucket source, ObjectInBucket target,
            Func<long, Task<bool>>? verifySize = null, bool targetIsOpen = false, CancellationToken token = default);
    }
}
