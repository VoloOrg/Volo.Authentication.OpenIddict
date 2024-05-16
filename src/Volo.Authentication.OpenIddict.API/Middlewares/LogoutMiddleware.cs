using Volo.Authentication.OpenIddict.API.Services;
using static Volo.Authentication.OpenIddict.API.Middlewares.MiddlewareHelpers;

namespace Volo.Authentication.OpenIddict.API.Middlewares
{
    public class LogoutMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAuthenticationClient _authenticationClient;

        public LogoutMiddleware(
            RequestDelegate next,
            IAuthenticationClient authenticationClient)
        {
            _next = next;
            _authenticationClient = authenticationClient;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Request.Cookies.TryGetValue("access_token", out var tokenForLogout);

            var response = await _authenticationClient.Logout(tokenForLogout);

            if (response.IsSuccessStatusCode)
            {
                DeleteCookies(context.Response);
                await GenerateResponse(context.Response, string.Empty, 200, string.Empty);
            }
            else
            {
                DeleteCookies(context.Response);
                await GenerateResponse(context.Response, string.Empty, 200, await response.Content.ReadAsStringAsync());
            }

            await _next(context);
        }
    }
}
