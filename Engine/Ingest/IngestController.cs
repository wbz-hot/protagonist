using System.Threading;
using System.Threading.Tasks;
using Engine.Ingest.Models;
using Engine.Messaging.Models;
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
            var result = await ingester.Ingest(message, cancellationToken);
            
            // TODO - format result based on success/failure of job
            
            return Ok(message);
        }
    }
}