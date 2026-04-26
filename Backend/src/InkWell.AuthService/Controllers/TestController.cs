using Microsoft.AspNetCore.Mvc;

namespace InkWell.AuthService.Controllers
{
    [ApiController]
    [Route("api/test")] // Base route as requested
    public class TestController : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            // Returns the exact message requested
            return Ok(new { message = "Auth Service is running" });
        }
    }
}
