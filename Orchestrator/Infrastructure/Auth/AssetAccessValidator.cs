﻿using System.Threading.Tasks;
using DLCS.Core.Guard;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Orchestrator.Assets;
using Orchestrator.Features.Auth;

namespace Orchestrator.Infrastructure.Auth
{
    /// <summary>
    /// Contains logic to validate passed BearerTokens and Cookies with a request for an asset.
    /// Setting status code and cookies depending on result of verification.
    /// </summary>
    public class AssetAccessValidator
    {
        private readonly ISessionAuthService sessionAuthService;
        private readonly AccessChecker accessChecker;
        private readonly AuthCookieManager cookieManager;
        private readonly IHttpContextAccessor httpContextAccessor;

        public AssetAccessValidator(
            ISessionAuthService sessionAuthService,
            AccessChecker accessChecker,
            AuthCookieManager cookieManager,
            IHttpContextAccessor httpContextAccessor)
        {
            this.sessionAuthService = sessionAuthService;
            this.accessChecker = accessChecker;
            this.cookieManager = cookieManager;
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Validate whether Bearer token associated with provided request has access to the specified resource.
        /// This will add SET-COOKIE header to response if the  asset is restricted and the current request has access.
        /// The Response StatusCode is NOT altered here - use the returned enum to do this downstream.
        /// </summary>
        /// <param name="orchestrationAsset">Current orchestration asset</param>
        /// <returns><see cref="AssetAccessResult"/> enum representing result of validation</returns>
        public async Task<AssetAccessResult> TryValidateBearerToken(OrchestrationAsset orchestrationAsset)
        {
            if (!orchestrationAsset.RequiresAuth) return AssetAccessResult.Open;

            var httpContext = httpContextAccessor.HttpContext.ThrowIfNull(nameof(httpContextAccessor.HttpContext))!;
            
            var bearerToken = GetBearerToken(httpContext.Request);
            if (string.IsNullOrEmpty(bearerToken))
            {
                // No bearer token, 401 but call underlying (e.g. to render info.json)
                return AssetAccessResult.Unauthorized;
            }
            
            // Get the authToken from bearerToken
            var customer = orchestrationAsset.AssetId.Customer;
            var authToken =
                await sessionAuthService.GetAuthTokenForBearerId(customer, bearerToken);

            if (authToken?.SessionUser == null)
            {
                // Bearer token not found, or expired, 401 but call underlying (e.g. to render info.json)
                return AssetAccessResult.Unauthorized;
            }
            
            // Validate current user has access for roles for requested asset
            var canAccess =
                await accessChecker.CanSessionUserAccessRoles(authToken.SessionUser, customer,
                    orchestrationAsset.Roles);

            if (!canAccess)
            {
                return AssetAccessResult.Unauthorized;
            }
            
            cookieManager.SetCookieInResponse(authToken);
            return AssetAccessResult.Authorized;
        }

        private string? GetBearerToken(HttpRequest httpRequest)
            => httpRequest.Headers.TryGetValue(HeaderNames.Authorization, out var authHeader)
                ? authHeader.ToString()?[7..] // everything after "Bearer "
                : null;
    }

    /// <summary>
    /// Enum representing various results for attempting to access an asset.
    /// </summary>
    public enum AssetAccessResult
    {
        /// <summary>
        /// Asset is open
        /// </summary>
        Open,
        
        /// <summary>
        /// Asset is restricted and current user does not have appropriate access
        /// </summary>
        Unauthorized,
        
        /// <summary>
        /// Asset is restricted and current user has access
        /// </summary>
        Authorized
    }
}