using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using DLCS.Core;
using DLCS.Core.Exceptions;
using DLCS.Model.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DLCS.Repository.Storage.S3
{
    public class S3BucketReader : IBucketReader
    {
        private readonly IAmazonS3 s3Client;
        private readonly IConfiguration configuration;
        private readonly ILogger<S3BucketReader> logger;

        // TODO - this implementation always assumes that bucket is in the same region
        public S3BucketReader(IAmazonS3 s3Client, IConfiguration configuration, ILogger<S3BucketReader> logger)
        {
            this.s3Client = s3Client;
            this.configuration = configuration;
            this.logger = logger;
        }

        public string DefaultRegion => configuration["AWS:Region"];

        public async Task<Stream?> GetObjectContentFromBucket(ObjectInBucket objectInBucket)
        {
            var getObjectRequest = objectInBucket.AsGetObjectRequest();
            try
            {
                GetObjectResponse getResponse = await s3Client.GetObjectAsync(getObjectRequest);
                return getResponse.ResponseStream;
            }
            catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation(e, "Could not find S3 object '{S3ObjectRequest}'", getObjectRequest.AsBucketAndKey());
                return Stream.Null;
            }
            catch (AmazonS3Exception e)
            {
                logger.LogWarning(e, "Could not copy S3 Stream for {S3ObjectRequest}; {StatusCode}",
                    getObjectRequest.AsBucketAndKey(), e.StatusCode);
                throw new HttpException(e.StatusCode, $"Error copying S3 stream for {getObjectRequest.AsBucketAndKey()}", e);
            }
        }
        
        public async Task<ObjectFromBucket> GetObjectFromBucket(ObjectInBucket objectInBucket)
        {
            var getObjectRequest = objectInBucket.AsGetObjectRequest();
            try
            {
                GetObjectResponse getResponse = await s3Client.GetObjectAsync(getObjectRequest);
                return getResponse.AsObjectInBucket(objectInBucket);
            }
            catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation(e, "Could not find S3 object '{S3ObjectRequest}'", getObjectRequest.AsBucketAndKey());
                return new ObjectFromBucket(objectInBucket, null, null);
            }
            catch (AmazonS3Exception e)
            {
                logger.LogWarning(e, "Could not copy S3 object for {S3ObjectRequest}; {StatusCode}",
                    getObjectRequest.AsBucketAndKey(), e.StatusCode);
                throw new HttpException(e.StatusCode, $"Error copying S3 stream for {getObjectRequest.AsBucketAndKey()}", e);
            }
        }

        public async Task<string[]> GetMatchingKeys(ObjectInBucket rootKey)
        {
            var listObjectsRequest = rootKey.AsListObjectsRequest();
            try
            {
                var response = await s3Client.ListObjectsAsync(listObjectsRequest, CancellationToken.None);
                return response.S3Objects.Select(obj => obj.Key).OrderBy(s => s).ToArray();
            }
            catch (AmazonS3Exception e)
            {
                logger.LogWarning(e, "Error getting matching keys {S3ListObjectRequest}; {StatusCode}",
                    listObjectsRequest, e.StatusCode);
                throw new HttpException(e.StatusCode, $"Error getting S3 objects for {listObjectsRequest}", e);
            }
        }

        public async Task<bool> CopyWithinBucket(string bucket, string sourceKey, string destKey)
        {
            logger.LogDebug("Copying {Source} to {Destination} in {Bucket}", sourceKey, destKey, bucket);
            try
            {
                CopyObjectRequest request = new CopyObjectRequest
                {
                    SourceBucket = bucket,
                    SourceKey = sourceKey,
                    DestinationBucket = bucket,
                    DestinationKey = destKey
                };
                CopyObjectResponse response = await s3Client.CopyObjectAsync(request);
                return true;
            }
            catch (AmazonS3Exception e)
            {
                logger.LogWarning(e, "Error encountered on server. Message:'{Message}' when writing an object",
                    e.Message);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Unknown encountered on server. Message:'{Message}' when writing an object",
                    e.Message);
            }

            return false;
        }

        public async Task<bool> WriteToBucket(ObjectInBucket dest, string content, string contentType)
        {
            try
            {
                // 1. Put object-specify only key name for the new object.
                var putRequest = new PutObjectRequest
                {
                    BucketName = dest.Bucket,
                    Key = dest.Key,
                    ContentBody = content,
                    ContentType = contentType
                };

                _ = await s3Client.PutObjectAsync(putRequest);
                return true;
            }
            catch (AmazonS3Exception e)
            {
                logger.LogWarning(e, "S3 Error encountered. Message:'{Message}' when writing an object to {key}",
                    e.Message, dest);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Unknown encountered on server. Message:'{Message}' when writing an object to {key}",
                    e.Message, dest);
            }

            return false;
        }

        public async Task<bool> WriteFileToBucket(ObjectInBucket dest, string filePath)
        {
            try
            {
                // 1. Put object-specify only key name for the new object.
                var putRequest = new PutObjectRequest
                {
                    BucketName = dest.Bucket,
                    Key = dest.Key,
                    FilePath = filePath,
                };

                _ = await s3Client.PutObjectAsync(putRequest);
                return true;
            }
            catch (AmazonS3Exception e)
            {
                logger.LogWarning(e, "S3 Error encountered. Message:'{Message}' when writing an object to {key}",
                    e.Message, dest);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Unknown encountered on server. Message:'{Message}' when writing an object to {key}",
                    e.Message, dest);
            }

            return false;
        }

        public async Task DeleteFromBucket(params ObjectInBucket[] toDelete)
        {
            try
            {
                var deleteObjectsRequest = new DeleteObjectsRequest
                {
                    BucketName = toDelete[0].Bucket,
                    Objects = toDelete.Select(oib => new KeyVersion{Key = oib.Key}).ToList(),
                };

                await s3Client.DeleteObjectsAsync(deleteObjectsRequest);
            }
            catch (AmazonS3Exception e)
            {
                logger.LogWarning(e, "S3 Error encountered. Message:'{Message}' when deleting objects from bucket",
                    e.Message);
            }
            catch (Exception e)
            {
                logger.LogWarning(e,
                    "Unknown encountered on server. Message:'{Message}' when deleting objects from bucket", e.Message);
            }
        }

        public async Task<bool> WriteLargeFileToBucket(ObjectInBucket dest, string filePath, string? contentType = null,
            CancellationToken token = default)
        {
            try
            {
                // 1. Put object-specify only key name for the new object.
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    BucketName = dest.Bucket,
                    Key = dest.Key,
                    FilePath = filePath,
                };

                if (!string.IsNullOrEmpty(contentType))
                {
                    uploadRequest.ContentType = contentType;
                }
                
                using var transferUtil = new TransferUtility(s3Client);
                await transferUtil.UploadAsync(uploadRequest, token);
                return true;
            }
            catch (AmazonS3Exception e)
            {
                logger.LogWarning(e, "S3 Error encountered writing large file to bucket. Key: '{key}'", dest);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Unknown error encountered writing large file to bucket. Key: '{key}'", dest);
            }

            return false;
        }

        /// <summary>
        /// Copy a large file (>5GiB) between buckets.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <remarks>See https://docs.aws.amazon.com/AmazonS3/latest/dev/CopyingObjctsUsingLLNetMPUapi.html </remarks>
        public async Task<ResultStatus<long?>> CopyLargeFileBetweenBuckets(ObjectInBucket source, ObjectInBucket target, CancellationToken token = default)
        {
            long objectSize = -1;
            var partSize = 5 * (long) Math.Pow(2, 20); // 5 MB
            var timer = Stopwatch.StartNew();
            var success = false;

            try
            {
                var initiateUploadTask = InitiateMultipartUpload(target);

                var sourceMetadata = await GetObjectMetadata(source);
                objectSize = sourceMetadata.ContentLength;

                var numberOfParts = Convert.ToInt32(objectSize / partSize);
                var copyResponses = new List<CopyPartResponse>(numberOfParts);

                var uploadId = await initiateUploadTask;

                long bytePosition = 0;
                for (int i = 1; bytePosition < objectSize; i++)
                {
                    var copyRequest = new CopyPartRequest
                    {
                        DestinationBucket = target.Bucket,
                        DestinationKey = target.Key,
                        SourceBucket = source.Bucket,
                        SourceKey = source.Key,
                        UploadId = uploadId,
                        FirstByte = bytePosition,
                        LastByte = bytePosition + partSize - 1 >= objectSize
                            ? objectSize - 1
                            : bytePosition + partSize - 1,
                        PartNumber = i
                    };

                    // TODO - do this in Task.WhenAll batches?
                    copyResponses.Add(await s3Client.CopyPartAsync(copyRequest, token));
                    bytePosition += partSize;

                    if (token.IsCancellationRequested)
                    {
                        logger.LogInformation("Cancellation requested, aborting multipart upload for {target}", target);
                        await s3Client.AbortMultipartUploadAsync(target.Bucket, target.Key, uploadId, token);
                        return ResultStatus<long?>.Unsuccessful(objectSize);
                    }
                }

                // Complete the request
                var completeRequest = new CompleteMultipartUploadRequest
                {
                    Key = target.Key,
                    BucketName = target.Bucket,
                    UploadId = uploadId,
                };
                completeRequest.AddPartETags(copyResponses);
                await s3Client.CompleteMultipartUploadAsync(completeRequest, token);
                success = true;
                return ResultStatus<long?>.Successful(objectSize);
            }
            catch (OverflowException e)
            {
                logger.LogError(e,
                    "Error getting number of parts to copy. From '{source}' to '{destination}'. Size {size}", source,
                    target, objectSize);
            }
            catch (AmazonS3Exception e)
            {
                logger.LogError(e,
                    "S3 Error encountered copying bucket-bucket item. From '{source}' to '{destination}'",
                    source, target);
            }
            catch (Exception e)
            {
                logger.LogError(e,
                    "Error during multipart bucket-bucket copy. From '{source}' to '{destination}'", source, target);
            }
            finally
            {
                timer.Stop();
                logger.LogInformation(
                    success
                        ? "Copied large file to '{target}' in {elapsed}ms."
                        : "Failed to copy large file to '{target}'. Failed after {elapsed}ms.",
                    target, timer.ElapsedMilliseconds);
            }
            
            return ResultStatus<long?>.Unsuccessful(objectSize);
        }

        private async Task<string> InitiateMultipartUpload(ObjectInBucket target)
        {
            var request = new InitiateMultipartUploadRequest {BucketName = target.Bucket, Key = target.Key};
            var response = await s3Client.InitiateMultipartUploadAsync(request);
            return response.UploadId;
        }

        private Task<GetObjectMetadataResponse> GetObjectMetadata(ObjectInBucket resource)
        {
            var request = resource.AsObjectMetadataRequest();
            return s3Client.GetObjectMetadataAsync(request);
        }
    }
}
