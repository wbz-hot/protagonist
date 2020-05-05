namespace Engine.Settings
{
    /// <summary>
    /// Settings directly related to A/V ingestion.
    /// </summary>
    /// <remarks>These will be for ElasticTranscoder</remarks>
    public class TimebasedIngestSettings
    {
        public string SourceBucket { get; set; }
        
        public string DestinationBucket { get; set; }
    }
}