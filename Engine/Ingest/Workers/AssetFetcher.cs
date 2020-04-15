using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using Engine.Ingest.Strategy;

namespace Engine.Ingest.Workers
{
    // TODO - name this better
    public class AssetFetcher : IAssetFetcher
    {
        private readonly ICustomerOriginRepository customerOriginRepository;
        private readonly Dictionary<OriginStrategy, IOriginStrategy> originStrategies;

        public AssetFetcher(
            ICustomerOriginRepository customerOriginRepository,
            IEnumerable<IOriginStrategy> originStrategies)
        {
            this.customerOriginRepository = customerOriginRepository;
            this.originStrategies = originStrategies.ToDictionary(k => k.Strategy, v => v);
        }
        
        // TODO - return something here? Final destination and image type/size? 
        public async Task CopyAssetFromOrigin(Asset asset, string destinationFolder,
            CancellationToken cancellationToken)
        {
            var customerOriginStrategy = await customerOriginRepository.GetCustomerOriginStrategy(asset);

            if (!originStrategies.TryGetValue(customerOriginStrategy.Strategy, out var strategy))
            {
                throw new InvalidOperationException(
                    $"No OriginStrategy found for '{customerOriginStrategy.Strategy}' strategy (id: {customerOriginStrategy.Id})");
            }

            var assetStream = await strategy.LoadAssetFromOrigin(asset, customerOriginStrategy, cancellationToken);
            
            /* TODO:
             - get origin strategies for customer
             - find if any match regex
             - get OriginStrategyImplementation based on this
             - call implementation to get image, whack it into storage
             - implementation may/may not need to copy depending on whether it is optimised?? (check)
             - should this handle ImageLocation too?
             */ 
            
            throw new System.NotImplementedException();
        }
    }
}