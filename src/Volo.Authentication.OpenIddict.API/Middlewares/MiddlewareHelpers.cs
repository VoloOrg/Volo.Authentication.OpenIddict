using System.Text.Json;
using System.Text;
using Volo.Authentication.OpenIddict.API.Models;

namespace Volo.Authentication.OpenIddict.API.Middlewares
{
    public static class MiddlewareHelpers
    {
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
