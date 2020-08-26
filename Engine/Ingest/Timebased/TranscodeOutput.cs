namespace Engine.Ingest.Timebased
{
    /// <summary>
    /// Represents 'Output' element of job transcode message
    /// </summary>
    public class TranscodeOutput
    {
        public string Id { get; set; }
        public string PresetId { get; set; }
        public string Key { get; set; }
        public string Status { get; set; }
        public long Duration { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}