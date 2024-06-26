﻿using Volo.Authentication.OpenIddict.Database;
using Volo.Authentication.OpenIddict.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using OpenIddictAbstraction = OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using AspNet.Security.OpenIdConnect.Extensions;
using System.Text.Json;
using Volo.Authentication.OpenIddict.Options;
using Microsoft.Extensions.Options;
using Volo.Authentication.OpenIddict.Mailing;

namespace Volo.Authentication.OpenIddict.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IMailingService _mailingService;
        private readonly MailingOptions _mailOptions;
        private static bool _databaseChecked;

        public AccountController(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext applicationDbContext,
            SignInManager<IdentityUser> signInManager,
            IOptions<MailingOptions> mailOptions,
            IMailingService mailingService)
        {
            _userManager = userManager;
            _applicationDbContext = applicationDbContext;
            _signInManager = signInManager;
            _mailOptions = mailOptions.Value;
            _mailingService = mailingService;
        }

        
        [HttpPost]
        [Route("Account/Register")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            EnsureDatabaseCreated(_applicationDbContext);
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    return StatusCode(StatusCodes.Status409Conflict);
                }

                //Add role checking logic
                

                if (!Role.AllRoles.Select(s => s.Id).Contains(model.Role))
                {
                    return StatusCode(StatusCodes.Status409Conflict);
                }

                user = new IdentityUser { UserName = model.Email, Email = model.Email };

                var result = await _userManager.CreateAsync(user, model.Password);
                
                if (result.Succeeded)
                {
                    var res = await _userManager.AddToRoleAsync(user, Role.AllRoles.First(r => r.Id == model.Role).Name);
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
        [Route("Account/InviteUser")]
        [Authorize(Roles = "Admin", AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public async Task<IActionResult> InviteUser([FromBody] InviteUserModel model)
        {
            var type = "InviteUser";

            EnsureDatabaseCreated(_applicationDbContext);
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    if(await _userManager.HasPasswordAsync(user))
                    {
                        return StatusCode(StatusCodes.Status409Conflict);
                    }
                    else
                    {
                        var token = await _userManager.GenerateUserTokenAsync(user, _userManager.Options.Tokens.PasswordResetTokenProvider, type);

                        string mailContent = _mailOptions.EnvironmentUri + _mailOptions.RegisterEndpoint + "?" + $"token={token}&email={model.Email}&type={type}&role={model.Role}";

                        var sendEmailModel = new SendEmailModel()
                        {
                            FromEmail = _mailOptions.FromEmail,
                            FromName = _mailOptions.FromName,
                            PlainTextMessage = mailContent,
                            HtmlTextMessage = string.Empty,
                            Subject = "invite user test",
                            ToEmail = model.Email,
                            ToName = model.Email
                        };

                        var mailResponse = await _mailingService.SendEmailAsync(sendEmailModel, CancellationToken.None);

                        return mailResponse.IsSuccess ? Ok() : BadRequest();
                    }
                }
                else
                {
                    //Add role checking logic
                    if (!Role.AllRoles.Select(s => s.Id).Contains(model.Role))
                    {
                        return StatusCode(StatusCodes.Status409Conflict);
                    }

                    user = new IdentityUser { UserName = model.Email, Email = model.Email };

                    var result = await _userManager.CreateAsync(user);

                    if (result.Succeeded)
                    {

                        var res = await _userManager.AddToRoleAsync(user, Role.AllRoles.First(r => r.Id == model.Role).Name);
                        if (res.Succeeded)
                        {
                            var token = await _userManager.GenerateUserTokenAsync(user, _userManager.Options.Tokens.PasswordResetTokenProvider, type);

                            string mailContent = _mailOptions.EnvironmentUri + _mailOptions.RegisterEndpoint + "?" + $"token={token}&email={model.Email}&type={type}&role={model.Role}";

                            var sendEmailModel = new SendEmailModel()
                            {
                                FromEmail = _mailOptions.FromEmail,
                                FromName = _mailOptions.FromName,
                                PlainTextMessage = mailContent,
                                HtmlTextMessage = string.Empty,
                                Subject = "invite user test",
                                ToEmail = model.Email,
                                ToName = model.Email
                            };

                            var mailResponse = await _mailingService.SendEmailAsync(sendEmailModel, CancellationToken.None);

                            return mailResponse.IsSuccess ? Ok() : BadRequest();
                        }
                    }

                    AddErrors(result);
                }                
            }

            // If we got this far, something failed.
            return BadRequest(ModelState);
        }

        [HttpPost]
        [Route("Account/changepassword")]
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel data, CancellationToken cancellationToken)
        {
            var email = OpenIddictAbstraction.OpenIddictExtensions.GetClaim(HttpContext.User,"email");

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
                var validationProblemDetails = new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        ["The old password is invalid."]
                });

                return ValidationProblem(validationProblemDetails);
            }

            if(data.NewPassword != data.ConfirmPassword)
            {
                var validationProblemDetails = new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        ["The new password and the confirm password don't match"]
                });

                return ValidationProblem(validationProblemDetails);
            }

            var newPasswordCheckResult = await _userManager.CheckPasswordAsync(user, data.NewPassword);

            if (newPasswordCheckResult)
            {
                var validationProblemDetails = new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        ["new password is not valid"]
                });

                return ValidationProblem(validationProblemDetails);
            }

            var result = await _userManager.ChangePasswordAsync(user, data.CurrentPassword, data.NewPassword);

            if (result.Succeeded)
            {
                return Ok();
            }

            // If we got this far, something failed.
            return BadRequest();
        }

        [HttpGet]
        [Route("Account/logout")]
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
