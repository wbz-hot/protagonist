﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orchestrator.Assets;
using Orchestrator.Features.Images.Commands;
using Orchestrator.Infrastructure.ReverseProxy;
using Orchestrator.Settings;
using Yarp.ReverseProxy.Forwarder;

namespace Orchestrator.Features.Images
{
    /// <summary>
    /// Route-to-code handlers for /iiif-img/ paths
    /// </summary>
    public static class ImageRouteHandlers
    {
        private static readonly HttpMessageInvoker HttpClient;
        private static readonly HttpTransformer DefaultTransformer;
        private static readonly ForwarderRequestConfig RequestOptions;

        static ImageRouteHandlers()
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
        /// Add endpoint mappings for /iiif-img/ paths
        /// </summary>
        /// <param name="endpoints">Current <see cref="IEndpointRouteBuilder"/> object.</param>
        public static void MapImageHandling(this IEndpointRouteBuilder endpoints)
        {
            var requestHandler = endpoints.ServiceProvider.GetService<ImageRequestHandler>();
            var forwarder = endpoints.ServiceProvider.GetService<IHttpForwarder>();
            var logger = endpoints.ServiceProvider.GetService<ILoggerFactory>()
                .CreateLogger(nameof(ImageRouteHandlers));
            var settings = endpoints.ServiceProvider.GetService<IOptions<ReverseProxySettings>>();
            
            endpoints.Map("/iiif-img/{customer}/{space}/{image}/{**assetRequest}", async httpContext =>
            {
                logger.LogDebug("Handling request '{Path}'", httpContext.Request.Path);
                var proxyResponse = await requestHandler.HandleRequest(httpContext);
                await ProcessResponse(logger, httpContext, forwarder, proxyResponse, settings);
            });
        }

        private static async Task ProcessResponse(ILogger logger, HttpContext httpContext, IHttpForwarder forwarder,
            IProxyActionResult proxyActionResult, IOptions<ReverseProxySettings> reverseProxySettings)
        {
            if (proxyActionResult is StatusCodeProxyResult statusCodeResult)
            {
                httpContext.Response.StatusCode = (int)statusCodeResult.StatusCode;
                foreach (var header in statusCodeResult.Headers)
                {
                    httpContext.Response.Headers.Add(header);
                }
                return;
            }

            if (proxyActionResult is OrchestrateImageResult orchestrateImageResult &&
                orchestrateImageResult.OrchestrationImage.Status != OrchestrationStatus.Orchestrated)
            {
                var mediator = httpContext.RequestServices.GetService<IMediator>();
                // Kick off orchestration logic here.
                // Once orchestrated, call image-server - by rewriting path
                // Where does path come from? Needs to know the location on disk

                // Should this return orchestrated path, on local disk?
                var result = await mediator.Send(new OrchestrateImage(orchestrateImageResult.OrchestrationImage),
                    httpContext.RequestAborted);

            }

            var proxyAction = proxyActionResult as ProxyActionResult; 
            var root = reverseProxySettings.Value.GetAddressForProxyTarget(proxyAction.Target).ToString();

            var transformer = proxyAction.HasPath
                ? new PathRewriteTransformer(proxyAction.Path)
                : DefaultTransformer;

            var error = await forwarder.SendAsync(httpContext, root, HttpClient, RequestOptions,
                transformer);
            
            // httpContext.Response.Body

            // Check if the proxy operation was successful
            if (error != ForwarderError.None)
            {
                var errorFeature = httpContext.Features.Get<IForwarderErrorFeature>();
                logger.LogError(errorFeature.Exception!, "Error in iiif-img direct handler for {Path}",
                    httpContext.Request.Path);
            }
        }
    }
}