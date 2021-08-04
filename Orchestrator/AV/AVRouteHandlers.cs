﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orchestrator.ReverseProxy;
using Yarp.ReverseProxy.Forwarder;

namespace Orchestrator.AV
{
    /// <summary>
    /// Route-to-code handlers for /iiif-av/ paths
    /// </summary>
    public static class AVRouteHandlers
    {
        private static readonly HttpMessageInvoker HttpClient;
        private static readonly HttpTransformer DefaultTransformer;
        private static readonly ForwarderRequestConfig RequestOptions;

        static AVRouteHandlers()
        {
            // TODO - should this be shared by AV + Image handling?
            HttpClient = new HttpMessageInvoker(new SocketsHttpHandler
            {
                UseProxy = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip,
                UseCookies = false
            });

            DefaultTransformer = HttpTransformer.Default;
            RequestOptions = new ForwarderRequestConfig {Timeout = TimeSpan.FromSeconds(100)};
        }
        
        /// <summary>
        /// Add endpoint mappings for /iiif-av/ paths
        /// </summary>
        /// <param name="endpoints">Current <see cref="IEndpointRouteBuilder"/> object.</param>
        public static void MapAVHandling(this IEndpointRouteBuilder endpoints)
        {
            var requestHandler = endpoints.ServiceProvider.GetService<AVRequestHandler>();
            var forwarder = endpoints.ServiceProvider.GetService<IHttpForwarder>();
            var logger = endpoints.ServiceProvider.GetService<ILoggerFactory>()
                .CreateLogger(nameof(AVRouteHandlers));

            endpoints.Map("/iiif-av/{customer}/{space}/{image}/{**assetRequest}", async httpContext =>
            {
                logger.LogDebug("Handling request '{Path}'", httpContext.Request.Path);
                var proxyResponse = await requestHandler.HandleRequest(httpContext);
                await ProxyRequest(logger, httpContext, forwarder, proxyResponse);
            });
        }

        private static async Task ProxyRequest(ILogger logger, HttpContext httpContext, IHttpForwarder forwarder,
            IProxyActionResult proxyActionResult)
        {
            if (proxyActionResult is StatusCodeProxyResult statusCodeResult)
            {
                httpContext.Response.StatusCode = (int) statusCodeResult.StatusCode;
                foreach (var header in statusCodeResult.Headers)
                {
                    httpContext.Response.Headers.Add(header);
                }
                return;
            }

            var error = await forwarder.SendAsync(httpContext, "http://127.0.0.1:8081", HttpClient, RequestOptions,
                DefaultTransformer);

            // Check if the proxy operation was successful
            if (error != ForwarderError.None)
            {
                var errorFeature = httpContext.Features.Get<IForwarderErrorFeature>();
                logger.LogError(errorFeature.Exception!, "Error in catch-all handler for {Path}",
                    httpContext.Request.Path);
            }
        }
    }
}