using DLCS.Model;

namespace DLCS.Repository.Entities
{
    /// <summary>
    /// Represents ImageOptimisationPolicy entity as stored in DLCS database. 
    /// </summary>
    public class ImageOptimisationPolicyEntity : IEntity
    {
        public string Id { get; set; }

        public string Name { get; set; }
        public string TechnicalDetails { get; set; }
        
        public void PrepareForDatabase()
        {
            // no-op
        }
    }
}