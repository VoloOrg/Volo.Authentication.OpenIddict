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
    [Route("api")]
    public class ResourceController : ControllerBase
    {
        [HttpGet("info")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Get()
        {
            var context = HttpContext;
            var email = User.GetClaim(Claims.Email);
            var name = User.GetClaim(Claims.Name);

            if (name is null && email is null)
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
            var content = new ResponseModel<InfoModel>()
            {
                Code = 200,
                Message = null,
                Data = new InfoModel() {Info = $"{name ?? "this user!"} with email: {email ?? "missing email!"} has been successfully authenticated." }
            };

            return Content(JsonConvert.SerializeObject(content), "application/json");
        }

        [HttpGet("IsLogedIn")]
        [Authorize]
        public async Task<IActionResult> IsUserLogedIn()
        {
            
            var content = new ResponseModel<bool>()
            {
                Code = 200,
                Message = null,
                Data = true
            };

            return Content(JsonConvert.SerializeObject(content), "application/json");
        }

        [HttpGet("currentUser")]
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
                Data = new CurrentUserModel() { Email =  email, Username = name, Roles = new List<string>() {role } },
            };

            return Content(JsonConvert.SerializeObject(content), "application/json");
        }

        [HttpGet("Public")]
        public async Task<IActionResult> Public()
        {
            var content = new ResponseModel<InfoModel>()
            {
                Code = 200,
                Message = null,
                Data = new InfoModel() { Info = "Public content" }
            };

            return Content(JsonConvert.SerializeObject(content), "application/json");
        }
    }
}
