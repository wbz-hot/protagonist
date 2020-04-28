using System;

namespace DLCS.Model.Customer
{
    public class CustomerStorage : IEntity
    {
        public int Customer { get; set; }
        public int Space { get; set; }
        public string? StoragePolicy { get; set; }
        public int NumberOfStoredImages { get; set; }
        public int TotalSizeOfStoredImages { get; set; }
        public int TotalSizeOfThumbnails { get; set; }
        public DateTime LastCalculated { get; set; }
        
        public void PrepareForDatabase()
        {
            //no-op
        }
    }
}