using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.ElasticTranscoder;
using Amazon.ElasticTranscoder.Model;
using Engine.Settings;
using Microsoft.Extensions.Options;

namespace Engine.Ingest.Timebased
{
    public class MediaTranscoder
    {
        private readonly IAmazonElasticTranscoder elasticTranscoder;
        private readonly IOptionsMonitor<TimebasedIngestSettings> timebasedSettings;

        public MediaTranscoder(IAmazonElasticTranscoder elasticTranscoder,
            IOptionsMonitor<TimebasedIngestSettings> timebasedSettings)
        {
            this.elasticTranscoder = elasticTranscoder;
            this.timebasedSettings = timebasedSettings;
        }
        
        public async Task InitiateTranscodeOperation(IngestionContext context, CancellationToken token = default)
        {
            var settings = timebasedSettings.CurrentValue;
            var getPipelineId = GetPipelineId(settings, token);

            var presets = await GetPresetIdLookup(token);
            
            var technicalDetails = context.Asset.FullImageOptimisationPolicy.TechnicalDetails;
            var outputs = new List<CreateJobOutput>(technicalDetails.Count);
            foreach (var technicalDetail in technicalDetails)
            {
                var output = "TODO - get path where this format will be output to";
                var presetName = settings.TranscoderMappings.TryGetValue(technicalDetail, out var mappedName)
                    ? mappedName
                    : technicalDetail;
                
                // TODO - handle not found
                var presetId = presets[presetName];

                outputs.Add(new CreateJobOutput
                {
                    PresetId = presetId,
                    Key = output,
                });
            }

            // TODO - throw if pipelineId not found

            // need pipeline id
            var pipelineId = await getPipelineId;
            var request = new CreateJobRequest
            {
                Input = new JobInput
                {
                    AspectRatio = "auto",
                    Container = "auto",
                    FrameRate = "auto",
                    Interlaced = "auto",
                    Resolution = "auto",
                    Key = context.AssetFromOrigin.Location // is the full key what we want here?
                },
                PipelineId = pipelineId,
                UserMetadata = new Dictionary<string, string>
                {
                    ["dlcsId"] = context.Asset.Id,
                    ["startTime"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ["jobId"] = Guid.NewGuid().ToString(), // do we want to pass this in for logging purposes?
                },
                Outputs = outputs
            };

            var response = await elasticTranscoder.CreateJobAsync(request, token);
            
            // TODO - return what here? success + size?
            
            throw new NotImplementedException();
        }

        private async Task<string?> GetPipelineId(TimebasedIngestSettings settings, CancellationToken token)
        {
            // TODO - cache this. Handle paging.
            var pipelines = await elasticTranscoder.ListPipelinesAsync(token);
            var pipeline = pipelines.Pipelines.FirstOrDefault(p => p.Name == settings.PipelineName);
            return pipeline?.Id;
        }

        private async Task<Dictionary<string, string>> GetPresetIdLookup(CancellationToken token)
        {
            var presets = new Dictionary<string, string>();
            var response = new ListPresetsResponse();

            // TODO cache this.
            do
            {
                var request = new ListPresetsRequest {PageToken = response.NextPageToken};
                response = await elasticTranscoder.ListPresetsAsync(request, token);

                foreach (var preset in response.Presets)
                {
                    presets.Add(preset.Name, preset.Id);
                }

            } while (response.NextPageToken != null);
            
            return presets;
        }
    }
}