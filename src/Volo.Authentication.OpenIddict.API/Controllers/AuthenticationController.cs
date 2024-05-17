using Volo.Authentication.OpenIddict.API.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using System.Text;
using static OpenIddict.Abstractions.OpenIddictConstants;
using System.Text.Json;
using Volo.Authentication.OpenIddict.API.Services;

namespace Volo.Authentication.OpenIddict.API.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationClient _authenticationClient;

        public AuthenticationController(
            IAuthenticationClient authenticationClient)
        {
            _authenticationClient = authenticationClient;
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
                Data = new CurrentUserModel() { Email = email, Role = Role.AllRoles.First(r => r.Name == role).Id },
            };

            return Ok(content);
        }

        [HttpPost("InviteUser")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> InviteUser([FromBody] InviteUserModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var token = Request.Headers["Authorization"].ToString();
           
            var response = await _authenticationClient.InviteUser(new StringContent(JsonSerializer.Serialize(model, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }), Encoding.UTF8, "application/json"), token);

            if (response.IsSuccessStatusCode)
            {
                var content = new ResponseModel<bool>()
                {
                    Code = 200,
                    Message = "mail is sent",
                    Data = true,
                };

                return Ok(content);
            }
            else
            {
                var content = new ResponseModel<bool>()
                {
                    Code = 400,
                    Message = "failed to send email",
                    Data = false,
                };

                return BadRequest(content);
            }
        }
    }
}
