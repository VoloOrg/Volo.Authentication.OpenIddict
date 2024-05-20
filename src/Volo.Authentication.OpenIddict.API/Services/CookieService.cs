namespace Volo.Authentication.OpenIddict.API.Services
{
    public class CookieService : ICookieService
    {
        private readonly IWebHostEnvironment _environment; 
        public CookieService(IWebHostEnvironment environment) 
        {
            _environment = environment;
        }

        public void DeleteCookies(HttpResponse httpResponse)
        {
            var options = new CookieOptions()
            {
                HttpOnly = true,
                Secure = true,
                IsEssential = true,
                SameSite = _environment.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Strict,
            };
            httpResponse.Cookies.Delete("access_token", options);
            httpResponse.Cookies.Delete("refresh_token", options);
        }

        public void AppendCookies(HttpResponse httpResponse, string accessToken, string refreshToken)
        {
            var options = new CookieOptions()
            {
                HttpOnly = true,
                Secure = true,
                IsEssential = true,
                SameSite = _environment.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(3600),
            };
            httpResponse.Cookies.Append("access_token", accessToken, options);
            httpResponse.Cookies.Append("refresh_token", refreshToken, options);
        }
    }
}
