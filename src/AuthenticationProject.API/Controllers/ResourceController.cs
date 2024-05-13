using AuthenticationProject.API.Models;
using AuthenticationProject.API.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Polly;
using System.Text;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthenticationProject.API.Controllers
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
