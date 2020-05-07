using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.ElasticTranscoder;
using Amazon.ElasticTranscoder.Model;
using Engine.Settings;
using LazyCache;
using Microsoft.Extensions.Options;
using TimeSpan = System.TimeSpan;

namespace Engine.Ingest.Timebased
{
    public class ElasticTranscoder : IMediaTranscoder
    {
        private readonly IAmazonElasticTranscoder elasticTranscoder;
        private readonly IAppCache cache;
        private readonly IOptionsMonitor<EngineSettings> engineSettings;

        public ElasticTranscoder(IAmazonElasticTranscoder elasticTranscoder,
            IAppCache cache,
            IOptionsMonitor<EngineSettings> engineSettings)
        {
            this.elasticTranscoder = elasticTranscoder;
            this.cache = cache;
            this.engineSettings = engineSettings;
        }
        
        public async Task<bool> InitiateTranscodeOperation(IngestionContext context, CancellationToken token = default)
        {
            var settings = engineSettings.CurrentValue.TimebasedIngest;
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

            var statusCode = (int) response.HttpStatusCode;
            return statusCode >= 200 && statusCode < 300;

            // TODO - return what here? success + size?
            // context.WithLocation(imageLocation).WithStorage(imageStorage); <- would this come after ingestion?
        }

        private Task<string?> GetPipelineId(TimebasedIngestSettings settings, CancellationToken token)
        {
            const string pipelinesKey = "MediaTranscode:PipelineId";

            return cache.GetOrAddAsync(pipelinesKey, async entry =>
            {
                // TODO - get ttl from cache
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                var response = new ListPipelinesResponse();

                do
                {
                    var request = new ListPipelinesRequest {PageToken = response.NextPageToken};
                    response = await elasticTranscoder.ListPipelinesAsync(request, token);

                    var pipeline = response.Pipelines.FirstOrDefault(p => p.Name == settings.PipelineName);
                    if (pipeline != null)
                    {
                        return pipeline.Id;
                    }

                } while (response.NextPageToken != null);

                return null; // TODO - handle not found
            });
        }

        private Task<Dictionary<string, string>> GetPresetIdLookup(CancellationToken token)
        {
            const string presetLookupKey = "MediaTranscode:Presets";

            return cache.GetOrAddAsync(presetLookupKey, async entry =>
            {
                // TODO - get ttl from cache
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                var presets = new Dictionary<string, string>();
                var response = new ListPresetsResponse();
                
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
            });
        }
    }
}