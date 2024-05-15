using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;
using AspNet.Security.OpenIdConnect.Primitives;
using Volo.Authentication.OpenIddict.Extentions;
using Volo.Authentication.OpenIddict.Models;

namespace Volo.Authentication.OpenIddict.Controllers
{
    
    public class AuthorizationController : ControllerBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AuthorizationController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpPost("connect/token"), IgnoreAntiforgeryToken, Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest();
            if (request.IsPasswordGrantType())
            {
                var user = await _userManager.FindByNameAsync(request.Username);
                if (user is null)
                {
                    var properties = new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The username/password couple is invalid."
                    });

                    return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                // Validate the username/password parameters and ensure the account is not locked out.
                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
                if (!result.Succeeded)
                {
                    var properties = new AuthenticationProperties(new Dictionary<string, string>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The username/password couple is invalid."
                    });

                    return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
                }

                // Create the claims-based identity that will be used by OpenIddict to generate tokens.
                var identity = new ClaimsIdentity(
                    authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                    nameType: Claims.Name,
                    roleType: Claims.Role);

                // Add the claims that will be persisted in the tokens.
                identity.SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user))
                        .SetClaim(Claims.Email, await _userManager.GetEmailAsync(user))
                        .SetClaim(Claims.Name, await _userManager.GetUserNameAsync(user))
                        .SetClaim(Claims.PreferredUsername, await _userManager.GetUserNameAsync(user))
                        .SetClaims(Claims.Role, [.. (await _userManager.GetRolesAsync(user))])
                        .SetClaim(Claims.Audience, "resource_server_1");
                
                var scopes = request.GetScopes();

                // Set the list of scopes granted to the client application.
                identity.SetScopes(new[]
                {
                Scopes.OpenId,
                Scopes.Email,
                Scopes.Profile,
                Scopes.Roles,
                Scopes.OfflineAccess,
                }.Intersect(request.GetScopes()));

                identity.SetDestinations(Helpers.GetDestinations);

                var signeIn = SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                return signeIn;
            }
            else if (request.IsRefreshTokenGrantType())
            {
                var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                var user = await _userManager.FindByIdAsync(result.Principal.GetClaim(Claims.Subject));

                if (user is null)
                {
                    return Forbid(
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: new AuthenticationProperties(new Dictionary<string, string>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid."
                        }));
                }

                // Ensure the user is still allowed to sign in.
                if (!await _signInManager.CanSignInAsync(user))
                {
                    return Forbid(
                        authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        properties: new AuthenticationProperties(new Dictionary<string, string>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
                        }));
                }

                // Create the claims-based identity that will be used by OpenIddict to generate tokens.
                var identity = new ClaimsIdentity(
                    authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                    nameType: Claims.Name,
                    roleType: Claims.Role);

                // Add the claims that will be persisted in the tokens.
                identity.SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user))
                        .SetClaim(Claims.Email, await _userManager.GetEmailAsync(user))
                        .SetClaim(Claims.Name, await _userManager.GetUserNameAsync(user))
                        .SetClaim(Claims.PreferredUsername, await _userManager.GetUserNameAsync(user))
                        .SetClaims(Claims.Role, [.. (await _userManager.GetRolesAsync(user))]);

                // Set the list of scopes granted to the client application.
                identity.SetScopes(new[]
                {
                Scopes.OpenId,
                Scopes.Email,
                Scopes.Profile,
                Scopes.Roles,
                Scopes.OfflineAccess,
                }.Intersect(request.GetScopes()));

                identity.SetDestinations(Helpers.GetDestinations);

                var signeIn = SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                return signeIn;
            }

            throw new NotImplementedException("The specified grant type is not implemented.");
        }


        [HttpGet]
        [Route("connect/logout")]
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Logout()
        {
            bool output = false;
            var email = HttpContext.User.GetClaim("email");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                var properties = new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "The username/password couple is invalid."
                });

                return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            
            var task = _signInManager.SignOutAsync();
            if (task.IsCompletedSuccessfully)
            {
                output = true;
                await _signInManager.SignOutAsync();
                SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            return output ? Ok() : BadRequest();
        }

        [HttpPost]
        [Route("connect/ForgotPassword")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordEmailModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return StatusCode(StatusCodes.Status422UnprocessableEntity, "Email was not valid");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            return new JsonResult(new ForgotPasswordResponseModel() { Token = token, Email = model.Email , Type = "ResetPassword" });
        }

        [HttpPost]
        [Route("connect/VerifyToken")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> VerifyToken([FromBody] VerifyTokenModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return StatusCode(StatusCodes.Status422UnprocessableEntity, "Email was not valid");
            }

            var isVerified = await _userManager.VerifyUserTokenAsync(user, _userManager.Options.Tokens.PasswordResetTokenProvider, model.Type, model.Token);

            if (isVerified)
            {
                return Ok();
            }

            return BadRequest();
        }


        [HttpPost]
        [Route("connect/ResetPassword")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return StatusCode(StatusCodes.Status422UnprocessableEntity, "Email was not valid");
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                return ValidationProblem();
            }

            var resetPassResult = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (!resetPassResult.Succeeded)
            {
                return BadRequest();
            }

            return Ok();
        }

        [HttpPost]
        [Route("connect/Register")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SetPassword([FromBody] ResetPasswordModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return StatusCode(StatusCodes.Status422UnprocessableEntity, "Email was not valid");
            }

            var isVerified = await _userManager.VerifyUserTokenAsync(user, _userManager.Options.Tokens.PasswordResetTokenProvider, "InviteUser", model.Token);

            if (!isVerified)
            {
                return StatusCode(StatusCodes.Status422UnprocessableEntity, "Token was not valid");
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                return ValidationProblem();
            }

            var resetPassResult = await _userManager.AddPasswordAsync(user, model.NewPassword);

            if (!resetPassResult.Succeeded)
            {
                return BadRequest();
            }

            return Ok();
        }
    }
}