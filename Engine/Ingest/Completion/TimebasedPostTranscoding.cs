using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Core;
using DLCS.Core.Guard;
using DLCS.Model.Assets;
using DLCS.Model.Storage;
using Engine.Ingest.Timebased;
using Engine.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Engine.Ingest.Completion
{
    public class TimebasedPostTranscoding : ITimebasedIngestorCompletion
    {
        private readonly IBucketReader bucketReader;
        private readonly TimebasedIngestSettings timebasedSettings;
        private readonly IAssetRepository assetRepository;
        private readonly ILogger<TimebasedPostTranscoding> logger;

        public TimebasedPostTranscoding(
            IBucketReader bucketReader,
            IOptionsMonitor<EngineSettings> engineSettings,
            IAssetRepository assetRepository,
            ILogger<TimebasedPostTranscoding> logger)
        {
            this.bucketReader = bucketReader;
            timebasedSettings = engineSettings.CurrentValue.TimebasedIngest;
            this.assetRepository = assetRepository;
            this.logger = logger;
        }

        /// <summary>
        /// Mark asset as completed in database and move assets from Transcode output to main location.
        /// </summary>
        public async Task<bool> CompleteIngestion(string assetId, TranscodeResult transcodeResult,
            CancellationToken cancellationToken)
        {
            var asset = await assetRepository.GetAsset(assetId);
            var assetIsOpen = asset.Roles.Count == 0;

            bool dimensionsUpdated = false;
            var transcodeOutputs = transcodeResult.Outputs;
            var copyTasks = new List<Task<ResultStatus<long?>>>(transcodeOutputs.Count);
            foreach (var transcodeOutput in transcodeOutputs)
            {
                SetAssetDimensions(asset, dimensionsUpdated, transcodeOutput);
                dimensionsUpdated = true;

                // Move assets from elastic transcoder-output bucket to main bucket
                copyTasks.Add(CopyTranscodedAssetToStorage(transcodeOutput, assetIsOpen, cancellationToken));
            }
            
            await DeleteInputFile(transcodeResult);

            var copyResults = await Task.WhenAll(copyTasks);

            var size = copyResults.Sum(result => result.Value ?? 0);
            var markAsIngestSuccess = await MarkAssetAsIngested(asset, size);

            // TODO - handle case where DB saved failed but copy had been successful. 2nd attempt the source files don't
            // exist so copyResults will be empty. Would need to read bucket metadata to see if copied files exist.
            // or would the whole thing just pass as successful to remove from retry?
            return copyResults.All(r => r.Success) && markAsIngestSuccess;
        }

        private void SetAssetDimensions(Asset asset, bool dimensionsUpdated, TranscodeOutput transcodeOutput)
        {
            var transcodeDuration = transcodeOutput.GetDuration();
            if (!dimensionsUpdated)
            {
                asset.Width = transcodeOutput.Width;
                asset.Height = transcodeOutput.Height;
                asset.Duration = transcodeDuration;
            }
            else if (transcodeDuration != asset.Duration)
            {
                logger.LogWarning("Asset {asset} has outputs with different durations: {duration1}ms and {duration2}ms",
                    asset.Id, asset.Duration, transcodeDuration);
            }
        }

        private async Task<ResultStatus<long?>> CopyTranscodedAssetToStorage(TranscodeOutput transcodeOutput,
            bool assetIsOpen, CancellationToken cancellationToken)
        {
            var key = transcodeOutput.Key;

            var source = new ObjectInBucket(timebasedSettings.OutputBucket, key);
            var destination = new ObjectInBucket(timebasedSettings.StorageBucket, key);

            var copyResult =
                await bucketReader.CopyLargeFileBetweenBuckets(source, destination, targetIsOpen: assetIsOpen,
                    token: cancellationToken);

            if (copyResult.Success)
            {
                // delete both in- and output buckets for ElasticTranscoder
                await bucketReader.DeleteFromBucket(source);
                logger.LogDebug("Successfully copied {output} to storage", key);
            }

            return copyResult;
        }
        
        private async Task DeleteInputFile(TranscodeResult transcodeResult)
        {
            if (string.IsNullOrEmpty(transcodeResult.InputKey)) return;
            
            var input = new ObjectInBucket(timebasedSettings.InputBucket, transcodeResult.InputKey);
            await bucketReader.DeleteFromBucket(input);
        }

        private async Task<bool> MarkAssetAsIngested(Asset asset, long assetSize)
        {
            try
            {
                var imageStore = new ImageStorage
                {
                    Id = asset.Id,
                    Customer = asset.Customer,
                    Space = asset.Space,
                    LastChecked = DateTime.Now,
                    Size = assetSize
                };

                // NOTE - ImageLocation isn't used for 'T', only 'I' family so just set an empty record
                var imageLocation = new ImageLocation {Id = asset.Id};
                
                var success =
                    await assetRepository.UpdateIngestedAsset(asset, imageLocation, imageStore);
                return success;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error marking AV asset as completed '{assetId}'", asset.Id);
                return false;
            }
        }
    }
}