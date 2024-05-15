using Volo.Authentication.OpenIddict.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Volo.Authentication.OpenIddict.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class ResourceController : ControllerBase
    {
        [HttpGet("Public")]
        public async Task<IActionResult> Public()
        {
            var content = new ResponseModel<InfoModel>()
            {
                Code = 200,
                Message = null,
                Data = new InfoModel() { Info = "Public content" }
            };

            return Ok(content);
        }
    }
}
