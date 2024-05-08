using AuthenticationProject.API.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AuthenticationProject.API.Controllers
{
    [ApiController]
    [Route("auth/account")]
    public class AuthenticationController : ControllerBase
    {
        [HttpGet("IsLogedIn")]
        public async Task<IActionResult> IsUserLogedIn()
        {
            var content = new ResponseModel<bool>()
            {
                Code = 200,
                Message = null,
                Data = User.Identity?.IsAuthenticated ?? false,
            };

            return Content(JsonConvert.SerializeObject(content), "application/json");
        }
    }
}
