using Volo.Authentication.OpenIddict.API.Options;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Nodes;
using Volo.Authentication.OpenIddict.API.Services;
using static Volo.Authentication.OpenIddict.API.Middlewares.MiddlewareHelpers;

namespace Volo.Authentication.OpenIddict.API.Middlewares
{
    public class AddTokenAsCookieMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AppInfoOptions _appInfoOptions;
        private readonly IAuthenticationClient _authenticationClient;

        public AddTokenAsCookieMiddleware(
            RequestDelegate next, 
            IOptions<AppInfoOptions> appInfoOptions,
            IAuthenticationClient authenticationClient)
        {
            
            _appInfoOptions = appInfoOptions.Value;
            _authenticationClient = authenticationClient;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            _ = context.Request.Cookies.TryGetValue("access_token", out var token);
            
            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(token);

            if(jwtSecurityToken is null || !jwtSecurityToken.Claims.Any(c => c.Type == "exp")) 
            {
                await GenerateResponse(context.Response, string.Empty, 401, "Expieration missing");
                return;
            }

            var tokenExpieres = DateTimeOffset.FromUnixTimeSeconds( long.Parse(jwtSecurityToken.Claims.First(t => t.Type == "exp").Value)).UtcDateTime;

            if(tokenExpieres.AddMinutes(-1) <= DateTime.UtcNow 
                && context.Request.Cookies.TryGetValue("refresh_token", out var cookieRefreshToken) 
                && !string.IsNullOrWhiteSpace(cookieRefreshToken))
            {
                var formData = new Dictionary<string, string>()
                {
                    { "grant_type", "refresh_token" },
                    { "refresh_token", cookieRefreshToken },
                    { "scope", _appInfoOptions.Scope },
                    { "client_id", _appInfoOptions.ClientId },
                    { "client_secret", _appInfoOptions.ClientSecret }
                }; 

                var response = await _authenticationClient.GetRefreshToken(new FormUrlEncodedContent(formData), token);

                if (response.IsSuccessStatusCode)
                {
                    string responseBodyString = await response.Content.ReadAsStringAsync();

                    var responceJson = JsonObject.Parse(responseBodyString);

                    var accessToken = responceJson["access_token"].ToString();
                    var refreshToken = responceJson["refresh_token"].ToString();

                    DeleteCookies(context.Response);
                    AppendCookies(context.Response, accessToken, refreshToken);

                    token = accessToken;
                }
                else
                {
                    await GenerateResponse(context.Response, string.Empty, 401, await response.Content.ReadAsStringAsync());
                }
            }

            context.Request.Headers.Remove("Authorization");
            context.Request.Headers.Add("Authorization", "Bearer " + token);
            await _next(context);
        }
    }
}
