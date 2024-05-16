using System.Text;
using Volo.Authentication.OpenIddict.API.Services;
using static Volo.Authentication.OpenIddict.API.Middlewares.MiddlewareHelpers;

namespace Volo.Authentication.OpenIddict.API.Middlewares
{
    public class ChangePasswordMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAuthenticationClient _authenticationClient;

        public ChangePasswordMiddleware(
            RequestDelegate next,
            IAuthenticationClient authenticationClient)
        {
            _next = next;
            _authenticationClient = authenticationClient;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            _ = context.Request.Cookies.TryGetValue("access_token", out var tokenForPassChange);

            string? requestBodyString = null;

            using (var reader = new StreamReader(context.Request.Body))
            {
                requestBodyString = await reader.ReadToEndAsync();
            }

            var response = await _authenticationClient.ChangePassword(new StringContent(requestBodyString, Encoding.UTF8, "application/json"), tokenForPassChange!);

            if (response.IsSuccessStatusCode)
            {
                await GenerateResponse(context.Response, true, 200, string.Empty);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                await GenerateResponse(context.Response, false, 400, "Incorrect information");
            }
            else
            {
                await GenerateResponse(context.Response, false, 403, await response.Content.ReadAsStringAsync());
            }
        }
    }
}
