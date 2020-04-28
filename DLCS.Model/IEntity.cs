namespace DLCS.Model
{
    /// <summary>
    /// Interface for marking entities that are saved to the database.
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// Prepares the entity for database write operations.
        /// </summary>
        /// <remarks>
        /// This can be for validation etc but the dlcs database has few nullable columns so some entities
        /// will need prepared prior to saving.
        /// </remarks>
        public void PrepareForDatabase();
    }
}