﻿using System.Collections.Generic;
using System.Threading.Tasks;
using DLCS.Model.Assets;

namespace DLCS.Model.Customer
{
    public interface ICustomerOriginRepository
    {
        public Task<IEnumerable<CustomerOriginStrategy>> GetCustomerOriginStrategies(int customer);
        
        /// <summary>
        /// Get <see cref="CustomerOriginStrategy"/> for specified <see cref="Asset"/>.
        /// </summary>
        /// <param name="asset">Asset to get <see cref="CustomerOriginStrategy"/> for.</param>
        /// <returns><see cref="CustomerOriginStrategy"/> to use for <see cref="Asset"/>.</returns>
        public Task<CustomerOriginStrategy> GetCustomerOriginStrategy(Asset asset);
    }
}