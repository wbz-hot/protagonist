﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Engine.Ingest
{
    [Route("image-ingest")]
    [ApiController]
    public class IngestController : Controller
    {
        [HttpPost]
        public Task<IActionResult> IngestImage([FromBody] dynamic payload)
        {
            return Task.FromResult(Ok("not-implemented") as IActionResult);
        }
    }
}