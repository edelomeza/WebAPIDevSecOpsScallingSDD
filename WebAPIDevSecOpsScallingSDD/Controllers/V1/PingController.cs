using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace WebAPIDevSecOpsScallingSDD.Controllers.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public sealed class PingController : ControllerBase
    {
        [HttpGet]
        public ActionResult<PingResponse> Get() => Ok(new PingResponse { Status = "Pong", Version = "v1" });
    }

    public sealed class PingResponse
    {
        public string Status { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;
    }
}
