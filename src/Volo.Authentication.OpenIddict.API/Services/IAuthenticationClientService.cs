namespace Volo.Authentication.OpenIddict.API.Services
{
    public interface IAuthenticationClientService
    {
        Task<HttpResponseMessage> GetToken(HttpContent content);
        Task<HttpResponseMessage> Logout(string? token);
        Task<HttpResponseMessage> ChangePassword(HttpContent content, string token);
        Task<HttpResponseMessage> RegisterByAdmin(HttpContent content, string token);
        Task<HttpResponseMessage> ForgetPassword(HttpContent content);
        Task<HttpResponseMessage> VerifyToken(HttpContent content);
        Task<HttpResponseMessage> ResetPassword(HttpContent content);
        Task<HttpResponseMessage> RegisterByUser(HttpContent content);
        Task<HttpResponseMessage> GetRefreshToken(HttpContent content, string token);

    }
}
