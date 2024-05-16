using static System.Net.WebRequestMethods;

namespace Volo.Authentication.OpenIddict.API.Middlewares
{
    public static class MiddlewareExtentions
    {
        public static WebApplication UseOpenIddictAuthentication(this WebApplication app)
        {
            app.MapWhen(context =>
                context.Request.Path == "/auth/connect/token"
                && context.Request.Method == Http.Post,
                (appBuilder) => appBuilder.UseMiddleware<TokenMiddleware>());
            app.MapWhen(context =>
                context.Request.Path == "/auth/account/logout"
                && context.Request.Method == Http.Get,
                (appBuilder) => appBuilder.UseMiddleware<LogoutMiddleware>());
            app.MapWhen(context =>
                context.Request.Path == "/auth/account/changepassword"
                && context.Request.Method == Http.Post
                && context.Request.Cookies.TryGetValue("access_token", out var tokenForPassChange)
                && !string.IsNullOrWhiteSpace(tokenForPassChange),
                (appBuilder) => appBuilder.UseMiddleware<ChangePasswordMiddleware>());

            //when the admin registers a user by predefined password
            app.MapWhen(context =>
                context.Request.Path == "/auth/account/register"
                && context.Request.Method == Http.Post
                && context.Request.Cookies.TryGetValue("access_token", out var tokenForRegister)
                && !string.IsNullOrWhiteSpace(tokenForRegister),
                (appBuilder) => appBuilder.UseMiddleware<RegisterByAdminMiddleware>());
            app.MapWhen(context =>
                context.Request.Path == "/auth/connect/ForgotPassword"
                && context.Request.Method == Http.Post
                && !context.Request.Cookies.ContainsKey("access_token"),
                (appBuilder) => appBuilder.UseMiddleware<ForgetPasswordMiddleware>());
            app.MapWhen(context =>
                context.Request.Path == "/auth/connect/VerifyToken"
                && context.Request.Method == Http.Post
                && !context.Request.Cookies.ContainsKey("access_token"),
                (appBuilder) => appBuilder.UseMiddleware<VerifyTokenMiddleware>());
            app.MapWhen(context =>
                context.Request.Path == "/auth/connect/ResetPassword"
                && context.Request.Method == Http.Post
                && !context.Request.Cookies.ContainsKey("access_token"),
                (appBuilder) => appBuilder.UseMiddleware<ResetPasswordMiddleware>());

            //when user is registering itself after invitation.
            app.MapWhen(context =>
                context.Request.Path == "/auth/connect/Register"
                && context.Request.Method == Http.Post
                && !context.Request.Cookies.ContainsKey("access_token"),
                (appBuilder) => appBuilder.UseMiddleware<RegisterByUserMiddleware>());

            //this middleware is for the rest of the operations
            app.UseWhen(context =>
                context.Request.Cookies.TryGetValue("access_token", out var token)
                && !string.IsNullOrWhiteSpace(token),
                (appBuilder) => appBuilder.UseMiddleware<AddTokenAsCookieMiddleware>());

            return app;
        }
    }
}
