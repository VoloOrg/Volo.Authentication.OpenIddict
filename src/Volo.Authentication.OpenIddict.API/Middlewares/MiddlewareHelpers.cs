using System.Text.Json;
using System.Text;
using Volo.Authentication.OpenIddict.API.Models;

namespace Volo.Authentication.OpenIddict.API.Middlewares
{
    public static class MiddlewareHelpers
    {
        public static void DeleteCookies(HttpResponse httpResponse)
        {
            var options = new CookieOptions()
            {
                HttpOnly = true,
                Secure = true,
                IsEssential = true,
                SameSite = SameSiteMode.Strict,
                Domain = ".azurewebsites.net"
            };
            httpResponse.Cookies.Delete("access_token", options);
            httpResponse.Cookies.Delete("refresh_token", options);
        }

        public static void AppendCookies(HttpResponse httpResponse, string accessToken, string refreshToken)
        {
            var options = new CookieOptions() 
            { 
                HttpOnly = true, 
                Secure = true, 
                IsEssential = true, 
                SameSite = SameSiteMode.Strict, 
                Expires = DateTime.UtcNow.AddMinutes(3600),
                Domain = ".azurewebsites.net"
            };
            httpResponse.Cookies.Append("access_token", accessToken, options);
            httpResponse.Cookies.Append("refresh_token", refreshToken, options);
        }

        public static async Task GenerateResponse<T>(HttpResponse httpResponse, T? data, int status, string? message)
        {
            var result = new ResponseModel<T>();
            result.Code = status;
            result.Data = data;
            result.Message = message;

            httpResponse.StatusCode = status;
            httpResponse.ContentType = "application/json";

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var bytes = Encoding.UTF8.GetBytes(json);
            await httpResponse.BodyWriter.WriteAsync(bytes);
        }
    }
}
