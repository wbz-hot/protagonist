﻿using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Model.Customer;
using DLCS.Model.Security;
using Microsoft.Extensions.Logging;

namespace Engine.Ingest.Strategy
{
    /// <summary>
    /// OriginStrategy implementation for 'basic-http-authentication' assets.
    /// </summary>
    public class BasicHttpAuthOriginStrategy : SafetyCheckOriginStrategy
    {
        private readonly HttpClient httpClient;
        private readonly ICredentialsRepository credentialsRepository;
        private readonly ILogger<BasicHttpAuthOriginStrategy> logger;
        
        public override OriginStrategy Strategy => OriginStrategy.BasicHttp;

        public BasicHttpAuthOriginStrategy(
            HttpClient httpClient,
            ICredentialsRepository credentialsRepository,
            ILogger<BasicHttpAuthOriginStrategy> logger)
        {
            this.httpClient = httpClient;
            this.credentialsRepository = credentialsRepository;
            this.logger = logger;
        }

        protected override async Task<OriginResponse?> LoadAssetFromOriginImpl(Asset asset,
            CustomerOriginStrategy customerOriginStrategy, CancellationToken cancellationToken = default)
        {
            var assetOrigin = asset.GetIngestOrigin();
            logger.LogDebug("Fetching asset from Origin: {url}", assetOrigin);

            try
            {
                var response = await GetHttpResponse(customerOriginStrategy, cancellationToken, assetOrigin);
                var originResponse = await CreateOriginResponse(response);
                return originResponse;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching asset from Origin: {url}", assetOrigin);
                return null;
            }
        }

        private async Task<HttpResponseMessage> GetHttpResponse(CustomerOriginStrategy customerOriginStrategy, CancellationToken cancellationToken,
            string assetOrigin)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, assetOrigin);
            request.Headers.Authorization = await SetBasicAuthHeader(customerOriginStrategy, request);
            var response = await httpClient.SendAsync(request, cancellationToken);
            return response;
        }

        private async Task<AuthenticationHeaderValue> SetBasicAuthHeader(CustomerOriginStrategy customerOriginStrategy, HttpRequestMessage request)
        {
            var basicCredentials =
                await credentialsRepository.GetBasicCredentialsForOriginStrategy(customerOriginStrategy);
            var creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{basicCredentials.User}:{basicCredentials.Password}"));
            return AuthenticationHeaderValue.Parse($"basic {creds}");
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