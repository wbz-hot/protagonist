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
        
        // TODO - timeouts, processing counts. May need more complex types.
    }
}