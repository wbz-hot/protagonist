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
        /// If true, use LocalRoot rather than AWS for messaging.
        /// </summary>
        public bool UseLocal { get; set; } = false;
        
        /// <summary>
        /// Root of locally running SQS instance.
        /// </summary>
        /// <remarks>E.g. http://localhost:4100 of using goaws</remarks>
        public string LocalRoot { get; set; }

        // TODO - timeouts, processing counts. May need more complex types.
    }
}