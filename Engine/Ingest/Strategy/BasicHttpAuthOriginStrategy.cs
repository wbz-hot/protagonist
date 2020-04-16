using System;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;

namespace Engine.Ingest.Strategy
{
    /// <summary>
    /// OriginStrategy implementation for 'basic-http-authentication' assets.
    /// </summary>
    public class BasicHttpAuthOriginStrategy : SafetyCheckOriginStrategy
    {
        public override OriginStrategy Strategy => OriginStrategy.BasicHttp;

        protected override Task<OriginResponse?> LoadAssetFromOriginImpl(Asset asset,
            CustomerOriginStrategy customerOriginStrategy, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}