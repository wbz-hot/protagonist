using System.Threading.Tasks;
using DLCS.Model.Customer;

namespace DLCS.Repository.Customer
{
    public class CustomerStorageRepository : ICustomerStorageRepository
    {
        public Task<bool> VerifyStoragePolicyBySize(int customer, int space, long proposedNewSize)
        {
            /*
             * IPolicyRepository.GetStoragePolicy
             * Check new size won't make to too large (lock to avoid threading issues)
             */
            throw new System.NotImplementedException();
        }

        public Task<bool> UpdateCustomerStorage(int customer, int space, long newFileSize)
        {
            /*
             * Update CustomerStorage records with new value.
             * Update both the individual Space record and the customer specific
             */
            throw new System.NotImplementedException();
        }
    }
}