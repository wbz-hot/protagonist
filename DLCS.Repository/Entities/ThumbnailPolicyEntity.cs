namespace DLCS.Repository.Entities
{
    /// <summary>
    /// Represents ThumbnailPolicy entity as stored in DLCS database. 
    /// </summary>
    internal class ThumbnailPolicyEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Sizes { get; set; }
    }
}