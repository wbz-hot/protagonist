using System.Threading.Tasks;

namespace DLCS.Model.Customer
{
    public interface ICustomerStorageRepository
    {
        /// <summary>
        /// Verify that the proposed new file size will not exceed storage policy limits.
        /// </summary>
        /// <param name="customer">CustomerId</param>
        /// <param name="proposedNewFileSize">The size, in bytes, of new image.</param>
        /// <returns>True if storage allowed, else false.</returns>
        Task<bool> VerifyStoragePolicyBySize(int customer, long proposedNewFileSize);

        Task<bool> UpdateCustomerStorage(int customer, int space, long newFileSize);
    }
}