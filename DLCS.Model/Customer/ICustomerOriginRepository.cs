using System.Collections.Generic;
using System.Threading.Tasks;

namespace DLCS.Model.Customer
{
    public interface ICustomerOriginRepository
    {
        public Task<IEnumerable<CustomerOriginStrategy>> GetCustomerOriginStrategy(int customer);
    }
}