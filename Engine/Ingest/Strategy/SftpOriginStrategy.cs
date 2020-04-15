using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;

namespace Engine.Ingest.Strategy
{
    /// <summary>
    /// OriginStrategy implementation for 'sftp' assets.
    /// </summary>
    public class SftpOriginStrategy : SafetyCheckOriginStrategy
    {
        public override OriginStrategy Strategy => OriginStrategy.SFTP;

        protected override Task<Stream> LoadAssetFromOriginImpl(Asset asset,
            CustomerOriginStrategy customerOriginStrategy, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}