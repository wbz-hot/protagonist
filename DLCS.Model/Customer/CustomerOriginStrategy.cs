﻿namespace DLCS.Model.Customer
{
    /// <summary>
    /// Represents an CustomerOriginStrategy used by DLCS system.
    /// </summary>
    public class CustomerOriginStrategy
    {
        public string Id { get; set; }
        public int Customer { get; set; }
        public string Regex { get; set; }
        public OriginStrategy Strategy { get; set; }
        public string Credentials { get; set; }
        public bool Optimised { get; set; }
    }
}