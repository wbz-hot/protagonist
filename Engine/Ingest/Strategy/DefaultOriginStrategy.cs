using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using Microsoft.Extensions.Logging;

namespace Engine.Ingest.Strategy
{
    /// <summary>
    /// OriginStrategy implementation for 'default' assets.
    /// </summary>
    public class DefaultOriginStrategy : SafetyCheckOriginStrategy
    {
        private readonly HttpClient httpClient;
        private readonly ILogger<DefaultOriginStrategy> logger;

        public DefaultOriginStrategy(HttpClient httpClient, ILogger<DefaultOriginStrategy> logger)
        {
            this.httpClient = httpClient;
            this.logger = logger;
        }
        
        public override OriginStrategy Strategy => OriginStrategy.Default;

        protected override async Task<Stream> LoadAssetFromOriginImpl(Asset asset,
            CustomerOriginStrategy customerOriginStrategy, CancellationToken cancellationToken = default)
        {
            // NOTE(DG): This will follow up to 8 redirections, as per deliverator.
            // However, https -> http will fail. 
            // Need to test relative redirects too.
            logger.LogDebug("Fetching asset from Origin: {url}", asset.Origin);

            try
            {
                var stream = await httpClient.GetStreamAsync(asset.Origin);
                return stream;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching asset from Origin: {url}", asset.Origin);
                return null;
            }
        }
    }
}