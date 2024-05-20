using System.Text.Json.Nodes;
using System.Text.Json;
using Volo.Authentication.OpenIddict.API.Models;
using Volo.Authentication.OpenIddict.API.Options;
using Microsoft.Extensions.Options;
using Volo.Authentication.OpenIddict.API.Services;
using static Volo.Authentication.OpenIddict.API.Middlewares.MiddlewareHelpers;

namespace Volo.Authentication.OpenIddict.API.Middlewares
{
    public class TokenMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AppInfoOptions _appInfoOptions;
        private readonly IAuthenticationClient _authenticationClient;
        private readonly ICookieService _cookieService;

        public TokenMiddleware(
            RequestDelegate next,
            IOptions<AppInfoOptions> appInfoOptions,
            IAuthenticationClient authenticationClient,
            ICookieService cookieService)
        {
            _next = next;
            _appInfoOptions = appInfoOptions.Value;
            _authenticationClient = authenticationClient;
            _cookieService = cookieService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string? requestBodyString = null;

            using (var reader = new StreamReader(context.Request.Body))
            {
                requestBodyString = await reader.ReadToEndAsync();
            }

            var body = JsonSerializer.Deserialize<LoginModel>(requestBodyString, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            Dictionary<string, string> parameters = new()
            {
                { "username", body.Email },
                { "password", body.Password },
                { "grant_type", "password" },
                { "scope", _appInfoOptions.Scope },
                { "client_id", _appInfoOptions.ClientId },
                { "client_secret", _appInfoOptions.ClientSecret }
            };

            var response = await _authenticationClient.GetToken(new FormUrlEncodedContent(parameters));


            if (response.IsSuccessStatusCode)
            {
                string responseBodyString = await response.Content.ReadAsStringAsync();

                var responceJson = JsonObject.Parse(responseBodyString);

                var token = responceJson["access_token"].ToString();
                var refreshToken = responceJson["refresh_token"].ToString();

                _cookieService.DeleteCookies(context.Response);
                _cookieService.AppendCookies(context.Response, token, refreshToken);

                await GenerateResponse(context.Response, string.Empty, 200, string.Empty);
            }
            else
            {
                await GenerateResponse(context.Response, string.Empty, (int)response.StatusCode, "Failed to log in");
            }
        }
    }
}
