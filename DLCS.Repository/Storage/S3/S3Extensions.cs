using Amazon.S3.Model;
using DLCS.Model.Storage;

namespace DLCS.Repository.Storage.S3
{
    public static class S3Extensions
    {
        public static GetObjectRequest AsGetObjectRequest(this ObjectInBucket resource) =>
            new GetObjectRequest
            {
                BucketName = resource.Bucket,
                Key = resource.Key
            };

        public static ListObjectsRequest AsListObjectsRequest(this ObjectInBucket resource) =>
            new ListObjectsRequest
            {
                BucketName = resource.Bucket,
                Prefix = resource.Key
            };

        public static string AsBucketAndKey(this GetObjectRequest getObjectRequest) =>
            $"{getObjectRequest.BucketName}/{getObjectRequest.Key}";

        public static ObjectFromBucket AsObjectInBucket(this GetObjectResponse getObjectResponse,
            ObjectInBucket objectInBucket)
            => new ObjectFromBucket(
                objectInBucket,
                getObjectResponse.ResponseStream,
                getObjectResponse.Headers.AsObjectInBucketHeaders()
            );

        public static ObjectInBucketHeaders AsObjectInBucketHeaders(this HeadersCollection headersCollection)
            => new ObjectInBucketHeaders
            {
                CacheControl = headersCollection.CacheControl,
                ContentDisposition = headersCollection.ContentDisposition,
                ContentEncoding = headersCollection.ContentEncoding,
                ContentLength = headersCollection.ContentLength == -1L ? (long?) null : headersCollection.ContentLength,
                ContentMD5 = headersCollection.ContentMD5,
                ContentType = headersCollection.ContentType,
                ExpiresUtc = headersCollection.ExpiresUtc
            };
    }
}
