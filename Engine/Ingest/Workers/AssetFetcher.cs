using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Core;
using DLCS.Core.Guard;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using Engine.Ingest.Strategy;
using Microsoft.Extensions.Logging;

namespace Engine.Ingest.Workers
{
    public class AssetFetcher : IAssetFetcher
    {
        private readonly ICustomerOriginRepository customerOriginRepository;
        private readonly ICustomerStorageRepository customerStorageRepository;
        private readonly ILogger<AssetFetcher> logger;
        private readonly Dictionary<OriginStrategy, IOriginStrategy> originStrategies;

        public AssetFetcher(
            ICustomerOriginRepository customerOriginRepository,
            IEnumerable<IOriginStrategy> originStrategies,
            ICustomerStorageRepository customerStorageRepository,
            ILogger<AssetFetcher> logger)
        {
             this.customerOriginRepository = customerOriginRepository;
             this.customerStorageRepository = customerStorageRepository;
             this.logger = logger;
             this.originStrategies = originStrategies.ToDictionary(k => k.Strategy, v => v);
        }

        public async Task<AssetFromOrigin> CopyAssetToBucket(Asset asset, string destinationTemplate, bool verifySize,
            bool fullBucketAccess, CancellationToken cancellationToken = default)
        {
            var customerOriginStrategy = await customerOriginRepository.GetCustomerOriginStrategy(asset, true);

            if (fullBucketAccess && customerOriginStrategy.Strategy == OriginStrategy.S3Ambient)
            {
                // copy S3-S3
                // use _something_ - IBucketReader?
            }
            
            // TODO - have a different implementation of IAssetFetcher?
            // TODO - this is all the same as the Image one.
            if (!originStrategies.TryGetValue(customerOriginStrategy.Strategy, out var strategy))
            {
                throw new InvalidOperationException(
                    $"No OriginStrategy found for '{customerOriginStrategy.Strategy}' strategy (id: {customerOriginStrategy.Id})");
            }
            
            // Copy to local disk
            await using var originResponse =
                await strategy.LoadAssetFromOrigin(asset, customerOriginStrategy, cancellationToken);
            
            if (originResponse == null || originResponse.Stream == Stream.Null)
            {
                // TODO correct type of exception?
                logger.LogWarning("Unable to get asset {assetId} from origin using {strategy}", asset.Id, asset.Origin,
                    strategy.Strategy);
                throw new ApplicationException($"Unable to get asset '{asset.Id}' from origin '{asset.Origin}'");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var assetFromOrigin = await CopyAssetToDisk(asset, destinationTemplate, originResponse);
            assetFromOrigin.CustomerOriginStrategy = customerOriginStrategy;
            
            // copy to S3
            
            throw new NotImplementedException();
        }

        public async Task<AssetFromOrigin> CopyAssetToDisk(Asset asset, string destinationTemplate, bool verifySize,
            CancellationToken cancellationToken = default)
        {
            destinationTemplate.ThrowIfNullOrWhiteSpace(nameof(destinationTemplate));

            var customerOriginStrategy = await customerOriginRepository.GetCustomerOriginStrategy(asset, true);

            if (!originStrategies.TryGetValue(customerOriginStrategy.Strategy, out var strategy))
            {
                throw new InvalidOperationException(
                    $"No OriginStrategy found for '{customerOriginStrategy.Strategy}' strategy (id: {customerOriginStrategy.Id})");
            }

            await using var originResponse =
                await strategy.LoadAssetFromOrigin(asset, customerOriginStrategy, cancellationToken);

            if (originResponse == null || originResponse.Stream == Stream.Null)
            {
                // TODO correct type of exception?
                logger.LogWarning("Unable to get asset {assetId} from origin using {strategy}", asset.Id, asset.Origin,
                    strategy.Strategy);
                throw new ApplicationException($"Unable to get asset '{asset.Id}' from origin '{asset.Origin}'");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var assetFromOrigin = await CopyAssetToDisk(asset, destinationTemplate, originResponse);
            assetFromOrigin.CustomerOriginStrategy = customerOriginStrategy;

            if (verifySize)
            {
                await VerifyFileSize(asset, assetFromOrigin);
            }
            
            return assetFromOrigin;
        }

        private async Task<AssetFromOrigin> CopyAssetToDisk(Asset asset, string destinationTemplate, OriginResponse originResponse)
        {
            TrySetContentTypeForBinary(originResponse, asset);
            
            var extension = GetFileExtension(originResponse);
            var targetPath = $"{destinationTemplate}.{extension}";

            if (File.Exists(targetPath))
            {
                logger.LogInformation("Target file '{file}' already exists, deleting", targetPath);
                File.Delete(targetPath);
            }

            try
            {
                var sw = Stopwatch.StartNew();
                await using var fileStream = new FileStream(targetPath, FileMode.OpenOrCreate, FileAccess.Write);
                var assetStream = originResponse.Stream;

                // If we have a contentLength, use that as received bytes and use framework to copy files
                bool knownFileSize = originResponse.ContentLength.HasValue;
                long received;
                
                if (knownFileSize)
                {
                    await assetStream.CopyToAsync(fileStream);
                    received = originResponse.ContentLength.Value;
                }
                else
                {
                    // NOTE(DG) This was copied from previous implementation, copies and works out size
                    received = await CopyToFileStream(assetStream, fileStream);
                }

                sw.Stop();

                logger.LogInformation(
                    "{assetId} to '{targetPath}': download done ({bytes} bytes, {elapsed}ms) using {copyType}",
                    asset.Id, targetPath, received, sw.ElapsedMilliseconds,
                    knownFileSize ? "framework-copy" : "manual-copy");

                return new AssetFromOrigin(asset.Id, received, targetPath, originResponse.ContentType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error writing file to disk. destination: {destination}", destinationTemplate);
                throw;
            }
        }
        
        private string GetFileExtension(OriginResponse originResponse)
        {
            var extension = MIMEHelper.GetExtensionForContentType(originResponse.ContentType);

            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = "file";
                logger.LogWarning("Unable to get a file extension for {contentType}",
                    originResponse.ContentType);
            }

            return extension;
        }

        private static async Task<long> CopyToFileStream(Stream assetStream, FileStream fileStream)
        {
            var buffer = new byte[102400];
            int size;
            long received = 0;

            while ((size = await assetStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, size);
                received += size;
                fileStream.Flush();
            }

            fileStream.Close();
            assetStream.Close();
            return received;
        }

        // TODO - this may need refined depending on whether it's 'I' or 'T' ingest
        private void TrySetContentTypeForBinary(OriginResponse originResponse, Asset asset)
        {
            var contentType = originResponse.ContentType;
            if (IsBinaryContent(contentType) || string.IsNullOrWhiteSpace(contentType))
            {
                var uniqueName = asset.GetUniqueName();
                var extension = uniqueName.Substring(uniqueName.LastIndexOf(".", StringComparison.Ordinal));

                var guess = MIMEHelper.GetContentTypeForExtension(extension);
                logger.LogInformation("Guessed content type as {contentType} for '{assetName}'", guess, uniqueName);
                originResponse.WithContentType(guess);
            }
        }

        private bool IsBinaryContent(string contentType) =>
            contentType == "application/octet-stream" ||
            contentType == "binary/octet-stream";

        private async Task VerifyFileSize(Asset asset, AssetFromOrigin assetFromOrigin)
        {
            // TODO - this might not need to happen, depending on whether the thing is whatever
            var customerHasEnoughSize = await customerStorageRepository.VerifyStoragePolicyBySize(asset.Customer,
                assetFromOrigin.AssetSize);

            if (!customerHasEnoughSize)
            {
                assetFromOrigin.FileTooLarge();
            }
        }
    }
}