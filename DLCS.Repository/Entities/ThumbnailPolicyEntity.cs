using DLCS.Model;

namespace DLCS.Repository.Entities
{
    /// <summary>
    /// Represents ThumbnailPolicy entity as stored in DLCS database. 
    /// </summary>
    internal class ThumbnailPolicyEntity : IEntity
    {
        public string Id { get; set; }

        public string Name { get; set; }
        public string Sizes { get; set; }
        
        public void PrepareForDatabase()
        {
            // no-op
        }
    }
}