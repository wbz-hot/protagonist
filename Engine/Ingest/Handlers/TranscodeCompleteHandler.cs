using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.ElasticTranscoder.Model;
using Engine.Ingest.Completion;
using Engine.Ingest.Timebased;
using Engine.Messaging;
using Engine.Messaging.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Engine.Ingest.Handlers
{
    /// <summary>
    /// Handler for Transcode Complete messages. 
    /// </summary>
    public class TranscodeCompleteHandler : IMessageHandler
    {
        private readonly ITimebasedIngestorCompletion timebasedIngestorCompletion;
        private readonly ILogger<TranscodeCompleteHandler> logger;
        
        public MessageType Type => MessageType.TranscodeComplete;

        public TranscodeCompleteHandler(
            ITimebasedIngestorCompletion timebasedIngestorCompletion,
            ILogger<TranscodeCompleteHandler> logger)
        {
            this.timebasedIngestorCompletion = timebasedIngestorCompletion;
            this.logger = logger;
        }
        
        public async Task<bool> Handle(QueueMessage message, CancellationToken cancellationToken)
        {
            var notification = DeserializeBody(message);

            if (notification == null) return false;

            if (!notification.UserMetadata.TryGetValue(UserMetadataKeys.DlcsId, out var assetId))
            {
                logger.LogWarning("Unable to find DlcsId in message for job {jobId}", notification.JobId);
                return false;
            }

            return await timebasedIngestorCompletion.CompleteIngestion(assetId, notification.Outputs,
                cancellationToken);
        }

        private ElasticTranscoderMessage? DeserializeBody(QueueMessage message)
        {
            try
            {
                var notification = JsonConvert.DeserializeObject<ElasticTranscoderNotification>(message.Body);
                return JsonConvert.DeserializeObject<ElasticTranscoderMessage>(notification.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deserializing message {message}", message.Body);
                return null;
            }
        }
    }

    /// <summary>
    /// Represents a notification that has been sent from AWS ElasticTranscoder via SNS.
    /// </summary>
    public class ElasticTranscoderNotification
    {
        public string Type { get; set; }
        public string MessageId { get; set; }
        public string TopicArn { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public string Timestamp { get; set; }
        public string SignatureVersion { get; set; }
        public string Signature { get; set; }
        public string SigningCertURL { get; set; }
        public string UnsubscribeURL { get; set; }
    }

    /// <summary>
    /// The body of a notification sent out from ElasticTranscoder.
    /// </summary>
    /// <remarks>See https://docs.aws.amazon.com/elastictranscoder/latest/developerguide/notifications.html</remarks>
    public class ElasticTranscoderMessage
    {
        public string State { get; set; }
        public string Version { get; set; }
        public string JobId { get; set; }
        public string PipelineId { get; set; }
        
        // Note - JobInput is from AWS nuget but is not used
        public JobInput Input { get; set; }
        public string? ErrorCode { get; set; }
        public string? OutputPrefix { get; set; }
        public int InputCount { get; set; }
        public List<TranscodeOutput> Outputs { get; set; }
        public Dictionary<string, string> UserMetadata { get; set; }
    }
}