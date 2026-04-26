using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InkWell.AuthService.Controllers
{
    [ApiController]
    [Route("api/secure")]
    public class SecureController : ControllerBase
    {
        // 1. Accessible by ANY logged-in user (regardless of specific role)
        // [Authorize] ensures the user has a valid JWT token
        [HttpGet("user")]
        [Authorize] 
        public IActionResult GetUserData()
        {
            return Ok(new InkWell.Shared.Responses.BaseResponse<object>(true, "User access granted", new { role = "User" }));
        }

        // 2. Accessible ONLY by users with the "Author" role
        // Returns 403 Forbidden if the user is a Reader or Admin but not Author
        [HttpGet("author")]
        [Authorize(Roles = "Author")] 
        public IActionResult GetAuthorData()
        {
            return Ok(new InkWell.Shared.Responses.BaseResponse<object>(true, "Author access granted", new { role = "Author" }));
        }

        // 3. Accessible ONLY by users with the "Admin" role
        // Returns 403 Forbidden if the user is a Reader or Author
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")] 
        public IActionResult GetAdminData()
        {
            return Ok(new InkWell.Shared.Responses.BaseResponse<object>(true, "Admin access granted", new { role = "Admin" }));
        }
    }
}
