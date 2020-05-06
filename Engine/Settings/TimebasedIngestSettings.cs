using System.Collections.Generic;

namespace Engine.Settings
{
    /// <summary>
    /// Settings directly related to A/V ingestion.
    /// </summary>
    /// <remarks>These will be for ElasticTranscoder</remarks>
    public class TimebasedIngestSettings
    {
        public string S3InputTemplate { get; set; }
        
        public string S3OutputTemplate { get; set; }

        public string PipelineName { get; set; }
        
        /// <summary>
        /// Mapping of 'friendly' to 'real' transcoder names
        /// </summary>
        public Dictionary<string, string> TranscoderMappings { get; set; } = new Dictionary<string, string>();
    }
}