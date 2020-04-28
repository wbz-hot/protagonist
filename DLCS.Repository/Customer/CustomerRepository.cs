using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using DLCS.Model.Customer;
using DLCS.Model.PathElements;

namespace DLCS.Repository.Customer
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly DatabaseAccessor databaseAccessor;

        public CustomerRepository(DatabaseAccessor databaseAccessor)
        {
            this.databaseAccessor = databaseAccessor;
        }

        public async Task<Dictionary<string, int>> GetCustomerIdLookup()
        {
            await using var connection = await databaseAccessor.GetOpenDbConnection();
            var results = await connection.QueryAsync<CustomerPathElement>("SELECT \"Id\", \"Name\" FROM \"Customers\"");
            return results.ToDictionary(cpe => cpe.Name, cpe => cpe.Id);
        }
    }
}
