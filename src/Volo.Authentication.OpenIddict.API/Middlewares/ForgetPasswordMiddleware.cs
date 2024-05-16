using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using Volo.Authentication.OpenIddict.API.Mailing;
using Volo.Authentication.OpenIddict.API.Options;
using Volo.Authentication.OpenIddict.API.Services;
using static Volo.Authentication.OpenIddict.API.Middlewares.MiddlewareHelpers;
using Volo.Authentication.OpenIddict.API.Models;

namespace Volo.Authentication.OpenIddict.API.Middlewares
{
    public class ForgetPasswordMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAuthenticationClient _authenticationClient;
        private readonly MailingOptions _mailOptions;
        private readonly IMailingService _mailingService;

        public ForgetPasswordMiddleware(
            RequestDelegate next,
            IAuthenticationClient authenticationClient,
            IOptions<MailingOptions> mailOptions,
            IMailingService mailingService)
        {
            _next = next;
            _authenticationClient = authenticationClient;
            _mailOptions = mailOptions.Value;
            _mailingService = mailingService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string requestBodyString = null;

            using (var reader = new StreamReader(context.Request.Body))
            {
                requestBodyString = await reader.ReadToEndAsync();
            }

            var response = await _authenticationClient.ForgetPassword(new StringContent(requestBodyString, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                //Email or other method to send the token
                var responseObject = JsonSerializer.Deserialize<ForgotPasswordResponseModel>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                string mailContent = _mailOptions.EnvironmentUri + _mailOptions.ResetEndpoint + "?" + $"token={responseObject.Token}&email={responseObject.Email}&type={responseObject.Type}";

                var sendEmailModel = new SendEmailModel()
                {
                    FromEmail = _mailOptions.FromEmail,
                    FromName = _mailOptions.FromName,
                    PlainTextMessage = mailContent,
                    HtmlTextMessage = string.Empty,
                    Subject = "reset password test",
                    ToEmail = responseObject.Email,
                    ToName = responseObject.Email
                };

                var mailResponse = await _mailingService.SendEmailAsync(sendEmailModel, CancellationToken.None);

                //for mailing service
                await GenerateResponse(context.Response, true, 200, "mail is sent");
                //for development sends token and email as responce
                //await GenerateResponse(context.Response, responseObject, 200, string.Empty);
            }
            else
            {
                await GenerateResponse(context.Response, true, 200, "mail is sent");
            }
        }
    }
}
