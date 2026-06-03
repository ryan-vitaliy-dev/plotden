using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Common
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Check()
        {
            // eventually add more health checking logic here, like dependencies and db stuff maybe
            return Ok();
        }
    }
}