namespace DLCS.Repository.Entities
{
    /// <summary>
    /// Represents CustomerOriginStrategy entity as stored in DLCS database.
    /// </summary>
    internal class CustomerOriginStrategyEntity
    {
        public string Id { get; set; }
        public int Customer { get; set; }
        public string Regex { get; set; }
        public string Strategy { get; set; }
        public string Credentials { get; set; }
        public bool Optimised { get; set; }
    }
}