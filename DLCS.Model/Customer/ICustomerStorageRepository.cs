using System.Threading.Tasks;

namespace DLCS.Model.Customer
{
    public interface ICustomerStorageRepository
    {
        Task<bool> VerifyStoragePolicyBySize(int customer, int space, long proposedNewSize);

        Task<bool> UpdateCustomerStorage(int customer, int space, long newFileSize);
    }
}