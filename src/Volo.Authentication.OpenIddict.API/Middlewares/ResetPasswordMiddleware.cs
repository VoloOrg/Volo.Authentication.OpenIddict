using System.Text;
using Volo.Authentication.OpenIddict.API.Services;
using static Volo.Authentication.OpenIddict.API.Middlewares.MiddlewareHelpers;

namespace Volo.Authentication.OpenIddict.API.Middlewares
{
    public class ResetPasswordMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAuthenticationClient _authenticationClient;

        public ResetPasswordMiddleware(
            RequestDelegate next,
            IAuthenticationClient authenticationClient)
        {
            _next = next;
            _authenticationClient = authenticationClient;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string requestBodyString = null;

            using (var reader = new StreamReader(context.Request.Body))
            {
                requestBodyString = await reader.ReadToEndAsync();
            }

            var response = await _authenticationClient.ResetPassword(new StringContent(requestBodyString, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                await GenerateResponse(context.Response, true, 200, "Password reseted successfully");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                await GenerateResponse(context.Response, false, 400, "Password/confirm password/token are wrong");
            }
            else
            {
                await GenerateResponse(context.Response, false, 400, "Password reset failed");
            }
        }
    }
}
