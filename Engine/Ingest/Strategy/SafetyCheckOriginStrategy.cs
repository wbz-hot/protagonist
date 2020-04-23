using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Core.Guard;
using DLCS.Model.Assets;
using DLCS.Model.Customer;

namespace Engine.Ingest.Strategy
{
    /// <summary>
    /// Implementation of <see cref="IOriginStrategy"/> that provides argument checking.
    /// </summary>
    public abstract class SafetyCheckOriginStrategy : IOriginStrategy
    {
        public abstract OriginStrategy Strategy { get; }

        protected abstract Task<OriginResponse?> LoadAssetFromOriginImpl(Asset asset,
            CustomerOriginStrategy customerOriginStrategy, CancellationToken cancellationToken = default);
        
        public Task<OriginResponse?> LoadAssetFromOrigin(Asset asset, CustomerOriginStrategy customerOriginStrategy,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            customerOriginStrategy.ThrowIfNull(nameof(customerOriginStrategy));
            asset.ThrowIfNull(nameof(asset));
                
            var originStrategy = customerOriginStrategy.Strategy;
            if (originStrategy != Strategy)
            {
                throw new InvalidOperationException(
                    $"Provided CustomerOriginStrategy uses strategy {originStrategy} which differs from current IOriginStrategy.Strategy '{Strategy}'");
            }

            return LoadAssetFromOriginImpl(asset, customerOriginStrategy, cancellationToken);
        }

        protected string GetOrigin(Asset asset) =>
            string.IsNullOrEmpty(asset.InitialOrigin) ? asset.Origin : asset.InitialOrigin;
    }
}