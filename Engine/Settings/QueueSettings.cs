namespace Engine.Settings
{
    public class QueueSettings
    {
        /// <summary>
        /// Basic image ingest queue.
        /// </summary>
        public string? Image { get; set; }
        
        /// <summary>
        /// Priority image ingest queue.
        /// </summary>
        public string? ImagePriority { get; set; }
        
        /// <summary>
        /// A/V ingest queue.
        /// </summary>
        public string? Video { get; set; }
        
        /// <summary>
        /// Queue for handling messages for A/V transcode completion.
        /// </summary>
        public string? VideoComplete { get; set; }

        /// <summary>
        /// If true, use LocalRoot rather than AWS for messaging.
        /// </summary>
        public bool UseLocal { get; set; } = false;
        
        /// <summary>
        /// Service root for SQS.
        /// </summary>
        public string ServiceRoot { get; set; }

        // TODO - timeouts, processing counts. May need more complex types.
    }
}