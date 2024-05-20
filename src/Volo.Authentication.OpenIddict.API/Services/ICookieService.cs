namespace Volo.Authentication.OpenIddict.API.Services
{
    public interface ICookieService
    {
        void DeleteCookies(HttpResponse httpResponse);
        void AppendCookies(HttpResponse httpResponse, string accessToken, string refreshToken);

    }
}
