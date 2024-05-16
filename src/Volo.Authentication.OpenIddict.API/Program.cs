using Volo.Authentication.OpenIddict.API.Mailing;
using Volo.Authentication.OpenIddict.API.Middlewares;
using Volo.Authentication.OpenIddict.API.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;
using SendGrid;
using SendGrid.Extensions.DependencyInjection;
using System.Text.Json;
using Volo.Authentication.OpenIddict.API.Services;

namespace Volo.Authentication.OpenIddict.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            
            builder.Services.AddControllers()
                .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

            builder.Services.AddOpenIddict()
                .AddValidation(options =>
                {
                    // Note: the validation handler uses OpenID Connect discovery
                    // to retrieve the issuer signing keys used to validate tokens.
                    options.SetIssuer(builder.Configuration.GetSection($"{AuthenticationOptions.Section}:AuthenticationUrl").Value);
                    options.AddAudiences(builder.Configuration.GetSection($"{AppInfoOptions.Section}:{nameof(AppInfoOptions.ClientId)}").Value);

                    options.UseIntrospection()
                            .SetClientId(builder.Configuration.GetSection($"{AppInfoOptions.Section}:{nameof(AppInfoOptions.ClientId)}").Value)
                            .SetClientSecret(builder.Configuration.GetSection($"{AppInfoOptions.Section}:{nameof(AppInfoOptions.ClientSecret)}").Value);

                    // Note: in a real world application, this encryption key should be
                    // stored in a safe place (e.g in Azure KeyVault, stored as a secret).
                    options.AddEncryptionKey(new SymmetricSecurityKey(
                        Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")));

                    // Register the System.Net.Http integration.
                    options.UseSystemNetHttp();

                    // Register the ASP.NET Core host.
                    options.UseAspNetCore();
                });

            builder.Services.Configure<AuthenticationOptions>(
                    builder.Configuration.GetSection(AuthenticationOptions.Section));
            builder.Services.Configure<AppInfoOptions>(
                    builder.Configuration.GetSection(AppInfoOptions.Section));

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("MyPolicy",
                                      policy =>
                                      {
                                          policy.WithOrigins(builder.Configuration.GetSection("AllowedCorsOrigins").Get<string[]>() ?? throw new InvalidOperationException("AllowedCorsOrigins configuration is null"))
                                                              .AllowAnyHeader()
                                                              .AllowAnyMethod()
                                                              .AllowCredentials();
                                      });
            });

            builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            builder.Services.AddAuthorization();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSendGrid(cfg => builder.Configuration.GetSection(nameof(SendGridClientOptions)).Bind(cfg));
            builder.Services.AddSingleton<IMailingService, MailingService>();
            builder.Services.Configure<MailingOptions>(
                    builder.Configuration.GetSection(MailingOptions.Section));

            builder.Services.AddHttpClient<IAuthenticationClient, AuthenticationClient>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseCors("MyPolicy");
            app.UseSwagger();
            app.UseSwaggerUI();

            //OpenIddict authentication middleware
            app.UseOpenIddictAuthentication();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
