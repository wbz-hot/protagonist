using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;

namespace Engine.Ingest.Strategy
{
    /// <summary>
    /// OriginStrategy implementation for 's3-ambient' assets.
    /// </summary>
    public class S3AmbientOriginStrategy : SafetyCheckOriginStrategy
    {
        public override OriginStrategy Strategy => OriginStrategy.S3Ambient;

        protected override Task<Stream> LoadAssetFromOriginImpl(Asset asset,
            CustomerOriginStrategy customerOriginStrategy, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}