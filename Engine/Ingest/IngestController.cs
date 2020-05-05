using System.Threading;
using System.Threading.Tasks;
using Engine.Ingest.Models;
using Microsoft.AspNetCore.Mvc;

namespace Engine.Ingest
{
    [Route("image-ingest")]
    [ApiController]
    public class IngestController : Controller
    {
        private readonly AssetIngester ingester;

        public IngestController(AssetIngester ingester)
        {
            this.ingester = ingester;
        }
        
        [HttpPost]
        public async Task<IActionResult> IngestImage([FromBody] IncomingIngestEvent message, CancellationToken cancellationToken)
        {
            // TODO - throw if this is a 'T' request
            var result = await ingester.Ingest(message, cancellationToken);

            return ConvertToStatusCode(message, result);
        }

        public IActionResult ConvertToStatusCode(IncomingIngestEvent message, IngestResult result)
            => result switch
            {
                IngestResult.Failed => StatusCode(500, message),
                IngestResult.Success => Ok(message),
                IngestResult.QueuedForProcessing => Accepted(message),
                IngestResult.Unknown => StatusCode(500, message),
                _ => StatusCode(500, message)
            };
    }
}