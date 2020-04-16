using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DLCS.Web.Handlers
{
    /// <summary>
    /// <see cref="DelegatingHandler"/> implementation that logs http request duration.
    /// </summary>
    public class RequestTimeLoggingHandler : DelegatingHandler
    {
        private readonly ILogger<RequestTimeLoggingHandler> logger;

        public RequestTimeLoggingHandler(ILogger<RequestTimeLoggingHandler> logger)
        {
            this.logger = logger;
        }
        
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var timer = Stopwatch.StartNew();
            HttpResponseMessage responseMessage = null;
            
            try
            {
                responseMessage = await base.SendAsync(request, cancellationToken);
            }
            finally
            {
                timer.Stop();
                var responseCode = responseMessage == null ? "<unknown>" : ((int)responseMessage.StatusCode).ToString();
                logger.LogDebug("Request to {url} completed with statusCode {statusCode} in {requestms}ms",
                    request.RequestUri, responseCode, timer.ElapsedMilliseconds);
            }

            return responseMessage;
        }
    }
}