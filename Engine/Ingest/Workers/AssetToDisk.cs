﻿using System;
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
    /// <summary>
    /// Class for copying asset from origin to local disk.
    /// </summary>
    public class AssetToDisk : IAssetMover
    {
        private readonly ICustomerStorageRepository customerStorageRepository;
        private readonly ILogger<AssetToDisk> logger;
        private readonly Dictionary<OriginStrategy, IOriginStrategy> originStrategies;

        public AssetToDisk(
            IEnumerable<IOriginStrategy> originStrategies,
            ICustomerStorageRepository customerStorageRepository,
            ILogger<AssetToDisk> logger)
        {
             this.customerStorageRepository = customerStorageRepository;
             this.logger = logger;
             this.originStrategies = originStrategies.ToDictionary(k => k.Strategy, v => v);
        }

        /// <summary>
        /// Copy asset from Origin to local disk.
        /// </summary>
        /// <param name="asset"><see cref="Asset"/> to be copied.</param>
        /// <param name="destinationTemplate">String representing destinations folder to copy to.</param>
        /// <param name="verifySize">if True, size is validated that it does not exceed allowed size.</param>
        /// <param name="customerOriginStrategy"><see cref="CustomerOriginStrategy"/> to use to fetch item.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns><see cref="AssetFromOrigin"/> containing new location, size etc</returns>
        public async Task<AssetFromOrigin> CopyAsset(
            Asset asset, 
            string destinationTemplate, 
            bool verifySize, 
            CustomerOriginStrategy customerOriginStrategy,
            CancellationToken cancellationToken = default)
        {
            destinationTemplate.ThrowIfNullOrWhiteSpace(nameof(destinationTemplate));

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
            var targetPath = $"{Path.Join(destinationTemplate, asset.GetUniqueName())}.{extension}";

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
                    // NOTE(DG) This was copied from previous Deliverator implementation, copies and works out size
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
            var customerHasEnoughSize = await customerStorageRepository.VerifyStoragePolicyBySize(asset.Customer,
                assetFromOrigin.AssetSize);

            if (!customerHasEnoughSize)
            {
                assetFromOrigin.FileTooLarge();
            }
        }
    }
}