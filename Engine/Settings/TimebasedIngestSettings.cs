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
        
        public string OutputBucket { get; set; }
        
        public string InputBucket { get; set; }
        
        public string StorageBucket { get; set; }

        /// <summary>
        /// The name of the pipeline to use for ingesting files.
        /// </summary>
        public string PipelineName { get; set; }
        
        /// <summary>
        /// The root processing folder where temporary files are placed.
        /// </summary>
        public string ProcessingFolder { get; set; }
        
        /// <summary>
        /// Template for location to download any assets to disk.
        /// </summary>
        public string SourceTemplate { get; set; }
        
        /// <summary>
        /// Mapping of 'friendly' to 'real' transcoder names
        /// </summary>
        public Dictionary<string, string> TranscoderMappings { get; set; } = new Dictionary<string, string>();
    }
}