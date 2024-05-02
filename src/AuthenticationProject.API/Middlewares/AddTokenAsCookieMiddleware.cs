using Newtonsoft.Json.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Threading;
using AuthenticationProject.API.Options;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace AuthenticationProject.API.Middlewares
{
    public class AddTokenAsCookieMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AuthenticationOptions _authenticationOptions;

        public AddTokenAsCookieMiddleware(RequestDelegate next, IOptions<AuthenticationOptions> authenticationOptions)
        {
            _authenticationOptions = authenticationOptions.Value;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/auth/connect/token")
            {
                HttpClient client = new HttpClient();

                var formData = await context.Request.ReadFormAsync();

                HttpRequestMessage request = new() 
                { 
                    RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "connect/token"), 
                    Method = new(HttpMethods.Post),
                    Content = new FormUrlEncodedContent(formData.Select(s => new KeyValuePair<string, string>(s.Key, s.Value.ToString()))),
                };

                var response = await client.SendAsync(request);

                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException ex)
                {
                    throw new HttpRequestException(await response.Content.ReadAsStringAsync(), ex, response.StatusCode);
                }

                
                if (response.IsSuccessStatusCode)
                {
                    string responseBodyString = await response.Content.ReadAsStringAsync();

                    var responceJson = JObject.Parse(responseBodyString);

                    var token = responceJson["access_token"].ToString();
                    var refreshToken = responceJson["refresh_token"].ToString();

                    context.Response.Cookies.Delete("access_token");
                    context.Response.Cookies.Delete("refresh_token");
                    context.Response.Cookies.Append("access_token", token);
                    context.Response.Cookies.Append("refresh_token", refreshToken);

                    context.Response.StatusCode = 200;
                }
                
            }
            else if (context.Request.Cookies.TryGetValue("access_token", out var token) && !string.IsNullOrWhiteSpace(token))
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtSecurityToken = handler.ReadJwtToken(token);

                if(jwtSecurityToken is null || !jwtSecurityToken.Claims.Any(c => c.Type == "exp")) 
                {
                    throw new UnauthorizedAccessException();
                }

                var tokenExpieres = DateTimeOffset.FromUnixTimeSeconds( long.Parse(jwtSecurityToken.Claims.First(t => t.Type == "exp").Value)).UtcDateTime;

                if(tokenExpieres.AddMinutes(-2) <= DateTime.UtcNow 
                    && context.Request.Cookies.TryGetValue("refresh_token", out var cookieRefreshToken) 
                    && !string.IsNullOrWhiteSpace(cookieRefreshToken))
                {
                    HttpClient client = new HttpClient();

                    var formData = new List<KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>
                    {
                        new KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>("grant_type", "refresh_token"),
                        new KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>("scope", "offline_access"),
                        new KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>("refresh_token", cookieRefreshToken),
                        new KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>("client_id", "resource_server_1"),
                        new KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>("client_secret", "846B62D0-DEF9-4215-A99D-86E6B8DAB342"),
                    };

                    HttpRequestMessage request = new()
                    {
                        RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "connect/token"),
                        Method = new(HttpMethods.Post),
                        Content = new FormUrlEncodedContent(formData.Select(s => new KeyValuePair<string, string>(s.Key, s.Value.ToString()))),
                    };

                    request.Headers.Remove("Authorization");
                    request.Headers.Add("Authorization", "Bearer " + token);

                    var response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBodyString = await response.Content.ReadAsStringAsync();

                        var responceJson = JObject.Parse(responseBodyString);

                        var accessToken = responceJson["access_token"].ToString();
                        var refreshToken = responceJson["refresh_token"].ToString();

                        context.Response.Cookies.Delete("access_token");
                        context.Response.Cookies.Delete("refresh_token");
                        context.Response.Cookies.Append("access_token", accessToken);
                        context.Response.Cookies.Append("refresh_token", refreshToken);

                        token = accessToken;

                        context.Response.StatusCode = 200;
                    }
                    else
                    {
                        throw new UnauthorizedAccessException("refresh token ended");
                    }
                }


                context.Request.Headers.Remove("Authorization");
                context.Request.Headers.Add("Authorization", "Bearer " + token);
                await _next(context);
            }
            else
            {
                throw new UnauthorizedAccessException("Narek unauthorized");
            }
        }
    }
}
