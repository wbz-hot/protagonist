﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using API.Client;
using DLCS.Core.Strings;
using DLCS.HydraModel;
using DLCS.Model.Customers;
using DLCS.Web.Auth;
using Hydra.Collections;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace API.Auth
{
    /// <summary>
    /// Auth in DLCS API
    ///  - Some paths, such as https://api.dlcs.io/ and various shared policies, can serve GET requests
    ///    to an anonymous user. Another example: https://api.dlcs.io/originStrategies
    ///  - An admin API key is allowed to make any valid API call.
    ///  - A customer path is one that starts /customers/n/, where n is the customer id.
    ///    Customer-specific resources such as images, auth services, etc live under these paths.
    ///  - Only an admin key or a key belonging to that customer can call these paths.
    ///  - A customer path is never anonymous.
    /// 
    /// Care should be taken not to confuse the customer whose resource is being called with the
    /// customer making the call.
    /// When the caller is admin, these can be different.
    /// The resource might not be associated with any customer.
    /// </summary>
    public class DlcsBasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationOptions>
    {
        private readonly ICustomerRepository customerRepository;
        private readonly DeliveratorApiAuth deliveratorApiAuth; // TODO - will become apiAuth

        public DlcsBasicAuthenticationHandler(
            IOptionsMonitor<BasicAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            ICustomerRepository customerRepository,
            DeliveratorApiAuth deliveratorApiAuth)
            : base(options, logger, encoder, clock)
        {
            this.customerRepository = customerRepository;
            this.deliveratorApiAuth = deliveratorApiAuth;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // skip authentication if endpoint has [AllowAnonymous] attribute
            // Not all API paths require auth...
            var endpoint = Context.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
                return AuthenticateResult.NoResult();

            // Added this for temporary debugging
            // if (Request.Method == "PATCH")
            // {
            //     var body = new StreamReader(Request.Body);
            //     var requestBody = await body.ReadToEndAsync();
            //     HydraCollection<Image>? b =
            //         JsonConvert.DeserializeObject<HydraCollection<Image>>(requestBody);
            //     // Logger.LogInformation(requestBody);
            // }
                
            
            
            // ...but any not marked must have the auth header
            if (!Request.Headers.ContainsKey("Authorization"))
                return AuthenticateResult.Fail("Missing Authorization Header in request");

            var headerValue = Request.GetAuthHeaderValue(AuthenticationHeaderUtils.BasicScheme);
            if (headerValue == null)
            {
                return AuthenticateResult.Fail("Missing Authorization Header in request");
            }

            // for a path like /customers/23/queue, the resourceCustomerId is 23.
            // This isn't necessarily the customer that owns the api key being used on the call!
            int? resourceCustomerId = null;
            if (Request.RouteValues.TryGetValue("customerId", out var customerIdRouteVal))
            {
                if (int.TryParse(customerIdRouteVal.ToString(), out int result))
                {
                    resourceCustomerId = result;
                }
            }
            var apiCaller = GetApiCaller(headerValue);
            if (apiCaller == null)
            {
                return AuthenticateResult.Fail("Invalid credentials");
            }
            
            var customerForKey = await customerRepository.GetCustomerForKey(apiCaller.Key, resourceCustomerId);
            if (customerForKey == null)
            {
                return AuthenticateResult.Fail("No customer found for this key that is permitted to access this resource");
            }

            if (apiCaller.Secret != deliveratorApiAuth.GetApiSecret(customerForKey, Options.Salt, apiCaller.Key))
            {
                return AuthenticateResult.Fail("Invalid credentials");
            }

            // We still have some checks to do before authenticating this user.
            // Some of these could be classed as authorisation rather than authentication, though they are not user-specific.
            if (resourceCustomerId.HasValue)
            {
                // the request is for a particular customer's resource (e.g., an asset)
                if (customerForKey.Id != resourceCustomerId.Value)
                {
                    // ... but the requester is not this customer. Only proceed if they are admin.
                    if (!customerForKey.Administrator)
                    {
                        return AuthenticateResult.Fail("Only admin user may access this customer's resource.");
                    }
                    Logger.LogInformation("Admin key accessing a customer's resource");
                }
            }
            else
            {
                // The request isn't for a customer resource (i.e., under the path /customers/n/).
                // Only proceed if they are admin.
                if (!customerForKey.Administrator)
                {
                    return AuthenticateResult.Fail("Only admin user may access this shared resource");
                }
                Logger.LogInformation("Admin key accessing a shared resource");
            }
            
            // At this point our *authentication* has passed.
            // Downstream handlers may still refuse to *authorise* the request for other reasons
            // (it can still end up a 401)
            Logger.LogInformation("Authentication passed");

            var claims = new List<Claim>
            {
                new (ClaimTypes.Name, customerForKey.Name), // TODO - should these be the other way round?
                new (ClaimTypes.NameIdentifier, apiCaller.Key), // ^^^
                new (ClaimsPrincipalUtils.Claims.Customer, customerForKey.Id.ToString()),
                new (ClaimTypes.Role, ClaimsPrincipalUtils.Roles.Customer),
            };
            if (customerForKey.Administrator)
            {
                claims.Add(new Claim(ClaimTypes.Role, ClaimsPrincipalUtils.Roles.Admin));
            }
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }

        private ApiCaller? GetApiCaller(AuthenticationHeaderValue headerValue)
        {
            try
            {
                var authHeader = headerValue.Parameter.DecodeBase64();
                string[] keyAndSecret = authHeader.Split(':');
                var apiCaller = new ApiCaller() {Key = keyAndSecret[0], Secret = keyAndSecret[1]};
                if (apiCaller.Key.HasText() && apiCaller.Secret.HasText())
                {
                    return apiCaller;
                }
                return null;
            }
            catch
            {
                Logger.LogError("Could not parse auth header");
            }
            return null;
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Headers["WWW-Authenticate"] = $"Basic realm=\"{Options.Realm}\"";
            return base.HandleChallengeAsync(properties);
        }
    }

    public class ApiCaller
    {
        public string Key { get; set; }
        public string Secret { get; set; }
    }
}