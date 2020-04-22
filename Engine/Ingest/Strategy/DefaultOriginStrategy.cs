﻿using System;
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

        protected override async Task<OriginResponse?> LoadAssetFromOriginImpl(Asset asset,
            CustomerOriginStrategy customerOriginStrategy, CancellationToken cancellationToken = default)
        {
            // NOTE(DG): This will follow up to 8 redirections, as per deliverator.
            // However, https -> http will fail. 
            // Need to test relative redirects too.
            logger.LogDebug("Fetching asset from Origin: {url}", asset.Origin);

            try
            {
                var assetOrigin = GetOrigin(asset);
                var response = await httpClient.GetAsync(assetOrigin, cancellationToken);
                var originResponse = await CreateOriginResponse(response);
                return originResponse;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching asset from Origin: {url}", asset.Origin);
                return null;
            }
        }

        private string GetOrigin(Asset asset)
        {
            var assetOrigin = string.IsNullOrEmpty(asset.InitialOrigin) ? asset.Origin : asset.InitialOrigin;
            logger.LogDebug("Using origin {assetOrigin} for asset {assetId}", assetOrigin, asset.Id);
            return assetOrigin;
        }

        private static async Task<OriginResponse> CreateOriginResponse(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            var content = response.Content;
            if (content == null)
            {
                return new OriginResponse(Stream.Null);
            }

            return new OriginResponse(await content.ReadAsStreamAsync())
                .WithContentLength(content.Headers.ContentLength)
                .WithContentType(content.Headers?.ContentType?.MediaType);
        }
    }
}