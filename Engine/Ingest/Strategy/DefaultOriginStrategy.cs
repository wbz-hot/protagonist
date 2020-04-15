using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;

namespace Engine.Ingest.Strategy
{
    /// <summary>
    /// OriginStrategy implementation for 'default' assets.
    /// </summary>
    public class DefaultOriginStrategy : SafetyCheckOriginStrategy
    {
        public override OriginStrategy Strategy => OriginStrategy.Default;

        protected override Task<Stream> LoadAssetFromOriginImpl(Asset asset,
            CustomerOriginStrategy customerOriginStrategy, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}