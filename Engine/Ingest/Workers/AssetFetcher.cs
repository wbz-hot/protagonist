using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using Engine.Ingest.Strategy;
using Microsoft.Extensions.Logging;

namespace Engine.Ingest.Workers
{
    // TODO - name this better
    public class AssetFetcher : IAssetFetcher
    {
        private readonly ICustomerOriginRepository customerOriginRepository;
        private readonly ILogger<AssetFetcher> logger;
        private readonly Dictionary<OriginStrategy, IOriginStrategy> originStrategies;

        public AssetFetcher(
            ICustomerOriginRepository customerOriginRepository,
            IEnumerable<IOriginStrategy> originStrategies,
            ILogger<AssetFetcher> logger)
        {
             this.customerOriginRepository = customerOriginRepository;
             this.logger = logger;
             this.originStrategies = originStrategies.ToDictionary(k => k.Strategy, v => v);
        }
        
        public async Task<AssetFromOrigin> CopyAssetFromOrigin(Asset asset, string destinationFolder,
            CancellationToken cancellationToken)
        {
            var customerOriginStrategy = await customerOriginRepository.GetCustomerOriginStrategy(asset);

            if (!originStrategies.TryGetValue(customerOriginStrategy.Strategy, out var strategy))
            {
                throw new InvalidOperationException(
                    $"No OriginStrategy found for '{customerOriginStrategy.Strategy}' strategy (id: {customerOriginStrategy.Id})");
            }
            
            await using var originResponse = await strategy.LoadAssetFromOrigin(asset, customerOriginStrategy, cancellationToken);
            
            if (originResponse == null)
            {
                // TODO correct type of exception?
                logger.LogWarning("Unable to get asset {assetId} from origin using {strategy}", asset.Id, asset.Origin,
                    strategy.Strategy);
                throw new ApplicationException($"Unable to get asset '{asset.Id}' from origin");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var result = await CopyAssetToDisk(asset, destinationFolder, originResponse);
            return result;

            /* TODO:
             - implementation may/may not need to copy depending on whether it is optimised?? (check)
             - should this handle ImageLocation too?
             - and/or ImageStorage?
             */
        }

        private async Task<AssetFromOrigin> CopyAssetToDisk(Asset asset, string destinationFolder, OriginResponse originResponse)
        {
            // TODO - is this unique name correct? Should it have path etc?
            var targetPath = Path.Combine(destinationFolder, asset.GetUniqueName());
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

                long received;
                if (originResponse.ContentLength.HasValue)
                {
                    // If we have a contentLength, use that and use framework to copy files
                    await assetStream.CopyToAsync(fileStream);
                    received = originResponse.ContentLength.Value;
                }
                else
                {
                    // NOTE(DG) This was copied from previous implementation, copies and works out size
                    received = await CopyToFileStream(assetStream, fileStream);
                }

                sw.Stop();

                logger.LogInformation("{customer}/{space}/{image}: download done ({bytes} bytes, {elapsed}ms",
                    asset.Customer, asset.Space, asset.GetUniqueName(), received, sw.ElapsedMilliseconds);

                return new AssetFromOrigin(asset.Id, received, targetPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error writing file to disk. destination: {destination}", destinationFolder);
                throw;
            }
        }

        private static async Task<long> CopyToFileStream(Stream assetStream, FileStream fileStream)
        {
            var buffer = new byte[102400];
            int size;
            long received = 0;

            while ((size = assetStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, size);
                received += size;
                fileStream.Flush();
            }

            fileStream.Close();
            assetStream.Close();
            return received;
        }
    }
}