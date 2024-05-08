using AuthenticationProject.API.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthenticationProject.API.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthenticationController : ControllerBase
    {
        [HttpGet("connect/IsLoggedIn")]
        public async Task<IActionResult> IsUserLoggedIn()
        {
            var content = new ResponseModel<bool>()
            {
                Code = 200,
                Message = null,
                Data = User.Identity?.IsAuthenticated ?? false,
            };

            return Content(JsonConvert.SerializeObject(content), "application/json");
        }

        [HttpGet("account/CurrentUser")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var email = User.GetClaim(Claims.Email);
            var name = User.GetClaim(Claims.Name);
            var role = User.GetClaim(Claims.Role);

            if (name is null || email is null || role is null)
            {
                return Challenge(
                    authenticationSchemes: OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictValidationAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                        [OpenIddictValidationAspNetCoreConstants.Properties.ErrorDescription] =
                            "The specified access token is bound to an account that no longer exists."
                    }));
            }
            var content = new ResponseModel<CurrentUserModel>()
            {
                Code = 200,
                Message = null,
                Data = new CurrentUserModel() { Email = email, Username = name, Roles = new List<string>() { role } },
            };

            return Content(JsonConvert.SerializeObject(content), "application/json");
        }
    }
}
