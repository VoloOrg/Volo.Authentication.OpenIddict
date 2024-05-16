using Microsoft.Extensions.Options;
using System.Text;
using Volo.Authentication.OpenIddict.API.Options;
using Volo.Authentication.OpenIddict.API.Services;
using static Volo.Authentication.OpenIddict.API.Middlewares.MiddlewareHelpers;

namespace Volo.Authentication.OpenIddict.API.Middlewares
{
    public class RegisterByAdminMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAuthenticationClient _authenticationClient;
        private readonly AuthenticationOptions _authenticationOptions;

        public RegisterByAdminMiddleware(
            RequestDelegate next,
            IAuthenticationClient authenticationClient,
            IOptions<AuthenticationOptions> authenticationOptions)
        {
            _next = next;
            _authenticationClient = authenticationClient;
            _authenticationOptions = authenticationOptions.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            _ = context.Request.Cookies.TryGetValue("access_token", out var tokenForRegister);
            string requestBodyString = null;

            using (var reader = new StreamReader(context.Request.Body))
            {
                requestBodyString = await reader.ReadToEndAsync();
            }

            HttpRequestMessage request = new()
            {
                RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "account/Register"),
                Method = new(HttpMethods.Post),
                Content = new StringContent(requestBodyString, Encoding.UTF8, "application/json"),
            };

            request.Headers.Remove("Authorization");
            request.Headers.Add("Authorization", "Bearer " + tokenForRegister);

            var response = await _authenticationClient.RegisterByAdmin(new StringContent(requestBodyString, Encoding.UTF8, "application/json"), tokenForRegister);

            if (response.IsSuccessStatusCode)
            {
                await GenerateResponse(context.Response, string.Empty, 200, string.Empty);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                await GenerateResponse(context.Response, string.Empty, 400, "incorrect information");
            }
            else
            {
                await GenerateResponse(context.Response, string.Empty, 403, await response.Content.ReadAsStringAsync());
            }
        }
    }
}
