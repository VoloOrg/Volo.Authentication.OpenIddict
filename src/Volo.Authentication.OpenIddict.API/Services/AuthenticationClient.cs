using Microsoft.Extensions.Options;
using Volo.Authentication.OpenIddict.API.Options;

namespace Volo.Authentication.OpenIddict.API.Services
{
    public class AuthenticationClient : IAuthenticationClient
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationOptions _authenticationOptions;

        public AuthenticationClient(HttpClient httpClient, IOptions<AuthenticationOptions> authenticationOptions)
        {
            _httpClient = httpClient;
            _authenticationOptions = authenticationOptions.Value;

            _httpClient.BaseAddress = new Uri(_authenticationOptions.AuthenticationUrl);
        }

        public async Task<HttpResponseMessage> GetToken(HttpContent content)
        {
            return await _httpClient.PostAsync("connect/token", content);
        }

        public async Task<HttpResponseMessage> Logout(string? token)
        {
            HttpRequestMessage request = new()
            {
                RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "account/logout"),
                Method = new(HttpMethods.Get)
            };

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Remove("Authorization");
                request.Headers.Add("Authorization", "Bearer " + token);
            }

            return await _httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> ChangePassword(HttpContent content, string token)
        {
            HttpRequestMessage request = new()
            {
                RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "account/changepassword"),
                Method = new(HttpMethods.Post),
                Content = content,
            };

            request.Headers.Remove("Authorization");
            request.Headers.Add("Authorization", "Bearer " + token);

            return await _httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> RegisterByAdmin(HttpContent content, string token)
        {
            HttpRequestMessage request = new()
            {
                RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "account/Register"),
                Method = new(HttpMethods.Post),
                Content = content,
            };

            request.Headers.Remove("Authorization");
            request.Headers.Add("Authorization", "Bearer " + token);

            return await _httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> ForgetPassword(HttpContent content)
        {
            HttpRequestMessage request = new()
            {
                RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "connect/ForgotPassword"),
                Method = new(HttpMethods.Post),
                Content = content,
            };

            return await _httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> VerifyToken(HttpContent content)
        {
            HttpRequestMessage request = new()
            {
                RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "connect/VerifyToken"),
                Method = new(HttpMethods.Post),
                Content = content,
            };

            return await _httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> ResetPassword(HttpContent content)
        {
            HttpRequestMessage request = new()
            {
                RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "connect/ResetPassword"),
                Method = new(HttpMethods.Post),
                Content = content,
            };

            return await _httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> RegisterByUser(HttpContent content)
        {
            HttpRequestMessage request = new()
            {
                RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "connect/Register"),
                Method = new(HttpMethods.Post),
                Content = content,
            };

            return await _httpClient.SendAsync(request);
        }


        public async Task<HttpResponseMessage> GetRefreshToken(HttpContent content, string token)
        {
            HttpRequestMessage request = new()
            {
                RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "connect/token"),
                Method = new(HttpMethods.Post),
                Content = content,
            };

            request.Headers.Remove("Authorization");
            request.Headers.Add("Authorization", "Bearer " + token);

            return await _httpClient.SendAsync(request);
        }
    }
}
