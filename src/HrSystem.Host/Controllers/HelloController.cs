using Microsoft.AspNetCore.Mvc;

namespace HrSystem.Host.Controllers
{
    [ApiController]
    [Route("api/hello")]
    public class HelloController : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult> hello()
        {
            return Ok("Hello World");
        }
    }
}
