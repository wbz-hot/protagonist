using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Engine.Tests.Ingest.Strategy
{
    /// <summary>
    /// Controllable HttpMessageHandler for unit testing HttpClient.
    /// </summary>
    public class ControllableHttpMessageHandler : HttpMessageHandler
    {
        private HttpResponseMessage response;
        public List<string> CallsMade = new List<string>();

        public HttpResponseMessage GetResponseMessage(string content, HttpStatusCode httpStatusCode)
        {
            var memStream = new MemoryStream();

            var sw = new StreamWriter(memStream);
            sw.Write(content);
            sw.Flush();
            memStream.Position = 0;

            var httpContent = new StreamContent(memStream);

            response = new HttpResponseMessage
            {
                StatusCode = httpStatusCode,
                Content = httpContent
            };
            return response;
        }

        public void SetResponse(HttpResponseMessage response) => this.response = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallsMade.Add(request.RequestUri.ToString());
            var tcs = new TaskCompletionSource<HttpResponseMessage>();
            tcs.SetResult(response);
            return tcs.Task;
        }
    }
}