using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Core;
using DLCS.Model.Storage;
using FluentAssertions.Execution;

namespace DLCS.Test.Helpers.Storage
{
    /// <summary>
    ///  Test bucket reader implementation that maintains in-memory list of addition/copy.
    /// </summary>
    /// <remarks>This saves tests knowing internal implementation.
    /// Uploading "file.json" 5 times should have same outcome as uploading once then copying.
    /// </remarks>
    public class TestBucketReader : IBucketReader
    {
        public Dictionary<string, BucketObject> Operations { get; } = new Dictionary<string, BucketObject>();
        private string forBucket;
        private List<string> verifiedPaths = new List<string>();

        public TestBucketReader(string bucket)
        {
            forBucket = bucket;
        }

        public TestBucketReader SeedPaths(string[] paths)
        {
            foreach (var path in paths)
            {
                Operations.Add(path, new BucketObject
                {
                    Contents = $"seed:{path}",
                    FilePath = $"seed:{path}"
                });
            }
            return this;
        }

        /// <summary>
        /// Assert key exists.
        /// </summary>
        public BucketObject ShouldHaveKey(string key)
        {
            if (Operations.TryGetValue(key, out var op))
            {
                verifiedPaths.Add(key);
                return op;
            }

            throw new AssertionFailedException($"{key} not found");
        }
        
        /// <summary>
        /// Assert key exists.
        /// </summary>
        public TestBucketReader ShouldNotHaveKey(string key)
        {
            if (Operations.TryGetValue(key, out var op))
            {
                throw new AssertionFailedException($"{key} found but should not exist");
            }

            return this;
        }

        /// <summary>
        /// Assert all keys have been verified.
        /// </summary>
        public void ShouldHaveNoUnverifiedPaths()
        {
            var unverified = Operations.Select(kvp => kvp.Key).Except(verifiedPaths);
            if (unverified.Any())
            {
                throw new AssertionFailedException($"The following paths have not been verified: {string.Join(",", unverified)}");
            }
        }

        public string DefaultRegion => "Fake-Region";

        public Task<Stream?> GetObjectContentFromBucket(ObjectInBucket objectInBucket)
        {
            throw new System.NotImplementedException();
        }

        public Task<ObjectFromBucket> GetObjectFromBucket(ObjectInBucket objectInBucket)
        {
            throw new NotImplementedException();
        }

        public Task<string[]> GetMatchingKeys(ObjectInBucket rootKey)
        {
            if (rootKey.Bucket != forBucket) throw new InvalidOperationException("Operation for different bucket");

            return Task.FromResult(Operations.Select(kvp => kvp.Key).ToArray());
        }

        public Task<bool> CopyWithinBucket(string bucket, string sourceKey, string destKey)
        {
            if (bucket != forBucket) throw new InvalidOperationException("Operation for different bucket");
            
            if (Operations.TryGetValue(sourceKey, out var op))
            {
                Operations[destKey] = new BucketObject {Contents = op.Contents, FilePath = op.FilePath};
                return Task.FromResult(true);
            }
            
            return Task.FromResult(false);
        }

        public Task<bool> WriteToBucket(ObjectInBucket dest, string content, string contentType)
        {
            if (dest.Bucket != forBucket) throw new InvalidOperationException("Operation for different bucket");

            Operations[dest.Key] = new BucketObject {Contents = content};
            return Task.FromResult(true);
        }

        public Task<bool> WriteFileToBucket(ObjectInBucket dest, string filePath)
        {
            if (dest.Bucket != forBucket) throw new InvalidOperationException("Operation for different bucket");

            Operations[dest.Key] = new BucketObject {FilePath = filePath};
            return Task.FromResult(true);
        }

        public Task DeleteFromBucket(params ObjectInBucket[] toDelete)
        {
            foreach (ObjectInBucket o in toDelete)
            {
                if (o.Bucket != forBucket) throw new InvalidOperationException("Operation for different bucket");

                if (Operations.ContainsKey(o.Key))
                {
                    Operations.Remove(o.Key);
                }
            }

            return Task.CompletedTask;
        }

        public Task<bool> WriteLargeFileToBucket(ObjectInBucket dest, string filePath, string? contentType = null, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<ResultStatus<long?>> CopyLargeFileBetweenBuckets(ObjectInBucket source, ObjectInBucket target, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public class BucketObject
        {
            public string FilePath { get; set; }
            public string Contents { get; set; }

            /// <summary>
            /// Assert object has expected file path.
            /// </summary>
            public BucketObject WithFilePath(string filePath)
            {
                if (FilePath != filePath)
                {
                    throw new AssertionFailedException($"FilePath expected {filePath} but was {FilePath}");
                }

                return this;
            }
            
            /// <summary>
            /// Assert object has expected contents.
            /// </summary>
            public BucketObject WithContents(string contents)
            {
                if (Contents != contents)
                {
                    throw new AssertionFailedException($"FilePath expected {contents} but was {Contents}");
                }

                return this;
            }
        }
    }
}