using System;
using System.Threading.Tasks;
using Dapper;
using DLCS.Core.Threading;
using DLCS.Model.Customer;
using DLCS.Model.Policies;
using DLCS.Repository.Policies;
using Microsoft.Extensions.Logging;

namespace DLCS.Repository.Customer
{
    public class CustomerStorageRepository : ICustomerStorageRepository
    {
        private readonly IPolicyRepository policyRepository;
        private readonly DatabaseAccessor databaseAccessor;
        private readonly ILogger<CustomerStorageRepository> logger;
        private readonly AsyncKeyedLock asyncLocker = new AsyncKeyedLock();

        public CustomerStorageRepository(IPolicyRepository policyRepository,
            DatabaseAccessor databaseAccessor,
            ILogger<CustomerStorageRepository> logger)
        {
            this.policyRepository = policyRepository;
            this.databaseAccessor = databaseAccessor;
            this.logger = logger;
        }
        
        public async Task<bool> VerifyStoragePolicyBySize(int customer, long proposedNewFileSize)
        {
            using var processLock = await asyncLocker.LockAsync(customer.ToString());
            try
            {
                var customerStorage = await GetCustomerStorageRecords(customer);
                if (string.IsNullOrEmpty(customerStorage.StoragePolicy))
                {
                    throw new ApplicationException(
                        $"Space 0 for Customer {customer} not found or does not have a storage policy");
                }

                var policy = await policyRepository.GetStoragePolicy(customerStorage.StoragePolicy);
                return customerStorage.TotalSizeOfStoredImages + proposedNewFileSize <=
                       policy.MaximumTotalSizeOfStoredImages;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error verifying storage policy for customer: {customer}. New size: {size}",
                    customer, proposedNewFileSize);
                return false;
            }
        }

        public Task<bool> UpdateCustomerStorage(int customer, int space, long newFileSize)
        {
            /*
             * Update CustomerStorage records with new value.
             * Update both the individual Space record and the customer specific
             */
            throw new System.NotImplementedException();
        }

        private async Task<CustomerStorage> GetCustomerStorageRecords(int customer, int? space = null)
        {
            var connection = await databaseAccessor.GetOpenDbConnection();
            return await connection.QueryFirstAsync<CustomerStorage>(GetCustomerStorageSql,
                new {Customer = customer, Space = space ?? 0});
        }
        
        private const string GetCustomerStorageSql =
            @"
SELECT ""Customer"", ""StoragePolicy"", ""NumberOfStoredImages"", ""TotalSizeOfStoredImages"", ""TotalSizeOfThumbnails"", ""LastCalculated"", ""Space""
FROM ""CustomerStorage""
WHERE ""Customer"" = @Customer AND ""Space"" = @Space";
    }
}