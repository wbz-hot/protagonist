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
            /* TODO:
            the origin strategy will have credentials
            these can come from S3, or be hardcoded
            fetch these (cache them?)
            use them to make the request to the downstream resource.
            */
            
            throw new NotImplementedException();
        }
    }
}