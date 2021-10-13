using DLCS.HydraModel;
using DLCS.HydraModel.Settings;
using DLCS.Mock.ApiApp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DLCS.Mock.Controllers
{
    [ApiController]
    public class DlcsApiController : ControllerBase
    {
        private readonly HydraSettings settings;
        
        public DlcsApiController(IOptions<HydraSettings> options)
        {
            settings = options.Value;
        }
        
        [HttpGet]
        [Route("/")]
        public EntryPoint Index()
        {
            var ep = new EntryPoint();
            ep.Init(settings, true);
            return ep;
        }
        
    }
}