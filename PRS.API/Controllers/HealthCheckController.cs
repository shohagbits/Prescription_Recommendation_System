using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PRS.Service;

namespace PRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthCheckController : ControllerBase
    {
        [HttpGet]
        [Route("status")]
        public async Task<IActionResult> Status()
        {
            return Ok($"API Health is Good at {DateTime.Now}!");
        }

    }
}
