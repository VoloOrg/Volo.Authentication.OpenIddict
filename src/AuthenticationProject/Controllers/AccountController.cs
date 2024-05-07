using AuthenticationProject.Database;
using AuthenticationProject.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthenticationProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private static bool _databaseChecked;

        public AccountController(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext applicationDbContext)
        {
            _userManager = userManager;
            _applicationDbContext = applicationDbContext;
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [Route("Account/Register")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            EnsureDatabaseCreated(_applicationDbContext);
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.UserName);
                if (user != null)
                {
                    return StatusCode(StatusCodes.Status409Conflict);
                }

                //Add role checking logic
                List<string> roles = ["Admin", "BCA", "EMU"];

                if (!roles.Contains(model.Role))
                {
                    return StatusCode(StatusCodes.Status409Conflict);
                }

                user = new IdentityUser { UserName = model.UserName, Email = model.UserName };

                var result = await _userManager.CreateAsync(user, model.Password);
                
                if (result.Succeeded)
                {
                    var res = await _userManager.AddToRoleAsync(user, model.Role);
                    if(res.Succeeded)
                    {
                        return Ok();
                    }
                }
                AddErrors(result);
            }

            // If we got this far, something failed.
            return BadRequest(ModelState);
        }


        [HttpPost]
        [Route("Account/changepassword")]
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel data, CancellationToken cancellationToken)
        {
            var email = OpenIddict.Abstractions.OpenIddictExtensions.GetClaim(HttpContext.User,"email");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                var properties = new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "The user is invalid."
                });

                return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // Validate the username/password parameters and ensure the account is not locked out.
            var oldPasswordCheckResult = await _userManager.CheckPasswordAsync(user, data.CurrentPassword);

            if (!oldPasswordCheckResult)
            {
                var properties = new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "The old password is invalid."
                });

                return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var newPasswordCheckResult = await _userManager.CheckPasswordAsync(user, data.NewPassword);

            if (newPasswordCheckResult)
            {
                var properties = new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "Old and new passwords cannot be the same."
                });

                return Forbid(properties, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var result = await _userManager.ChangePasswordAsync(user, data.CurrentPassword, data.NewPassword);

            if (result.Succeeded)
            {
                return Ok();
            }

            // If we got this far, something failed.
            return BadRequest();
        }


        [HttpPost]
        [Route("Account/ForgotPassword")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordEmailModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return StatusCode(StatusCodes.Status422UnprocessableEntity,"Email was not valid");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            return new JsonResult(new ForgotPasswordResponseModel (){ Token = token, Email = model.Email });
        }


        [HttpPost]
        [Route("Account/ResetPassword")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if(model.Password != model.ConfirmPassword)
            {
                return StatusCode(StatusCodes.Status422UnprocessableEntity, "Confirm Password was not valid");
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return StatusCode(StatusCodes.Status422UnprocessableEntity, "Email was not valid");
            }

            var resetPassResult = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

            if (!resetPassResult.Succeeded)
            {
                return BadRequest();
            }

            return Ok();
        }


        #region Helpers

        // The following code creates the database and schema if they don't exist.
        // This is a temporary workaround since deploying database through EF migrations is
        // not yet supported in this release.
        // Please see this http://go.microsoft.com/fwlink/?LinkID=615859 for more information on how to do deploy the database
        // when publishing your application.
        private static void EnsureDatabaseCreated(ApplicationDbContext context)
        {
            if (!_databaseChecked)
            {
                _databaseChecked = true;
                context.Database.EnsureCreated();
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        #endregion
    }
}
