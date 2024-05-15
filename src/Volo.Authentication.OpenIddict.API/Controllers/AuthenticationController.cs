using AuthenticationOpenIddict.API.Mailing;
using AuthenticationOpenIddict.API.Models;
using AuthenticationOpenIddict.API.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using System.Text;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthenticationOpenIddict.API.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthenticationController : ControllerBase
    {
        private readonly Options.AuthenticationOptions _authenticationOptions;
        private readonly MailingOptions _mailOptions;
        private readonly IMailingService _mailingService;

        public AuthenticationController(
            IOptions<Options.AuthenticationOptions> authenticationOptions, 
            IOptions<MailingOptions> mailOptions,
            IMailingService mailingService)
        {
            _authenticationOptions = authenticationOptions.Value;
            _mailOptions = mailOptions.Value;
            _mailingService = mailingService;
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

            HttpClient client = new HttpClient();
            
            var token = Request.Headers["Authorization"].ToString();
            
            HttpRequestMessage request = new()
            {
                RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "account/InviteUser"),
                Method = new(HttpMethods.Post),
                Content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Authorization", token);

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                //Email or other method to send the token
                var responseObject = JsonConvert.DeserializeObject<InviteUserResponseModel>(await response.Content.ReadAsStringAsync());

                string mailContent = _mailOptions.EnvironmentUri + _mailOptions.RegisterEndpoint + "?" + $"token={responseObject.Token}&email={responseObject.Email}&type={responseObject.Type}&role={responseObject.Role}";

                var sendEmailModel = new SendEmailModel()
                {
                    FromEmail = _mailOptions.FromEmail,
                    FromName = _mailOptions.FromName,
                    PlainTextMessage = mailContent,
                    HtmlTextMessage = string.Empty,
                    Subject = "invite user test",
                    ToEmail = responseObject.Email,
                    ToName = responseObject.Email
                };

                var mailResponse = await _mailingService.SendEmailAsync(sendEmailModel, CancellationToken.None);

                var content = new ResponseModel<bool>()
                {
                    Code = mailResponse.IsSuccess ? 200 : 400,
                    Message = mailResponse.IsSuccess ? "mail is sent" : "failed to send email",
                    Data = mailResponse.IsSuccess,
                };

                return mailResponse.IsSuccess ? Ok(content) : BadRequest(content);
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
