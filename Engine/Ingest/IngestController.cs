using System.Threading.Tasks;
using Engine.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace Engine.Ingest
{
    [Route("image-ingest")]
    [ApiController]
    public class IngestController : Controller
    {
        [HttpPost]
        public Task<IActionResult> IngestImage([FromBody] IngestEvent message)
        {
            return Task.FromResult(Ok(message) as IActionResult);
        }
    }
}