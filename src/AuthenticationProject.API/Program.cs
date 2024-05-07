using AuthenticationProject.API.EmailService;
using AuthenticationProject.API.Middlewares;
using AuthenticationProject.API.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;

namespace AuthenticationProject.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            
            builder.Services.AddControllers();

            builder.Services.AddOpenIddict()
                .AddValidation(options =>
                {
                    // Note: the validation handler uses OpenID Connect discovery
                    // to retrieve the issuer signing keys used to validate tokens.
                    options.SetIssuer(builder.Configuration.GetSection($"{AuthenticationOptions.Section}:AuthenticationUrl").Value);
                    options.AddAudiences("resource_server_1");

                    options.UseIntrospection()
                            .SetClientId("resource_server_1")
                            .SetClientSecret("846B62D0-DEF9-4215-A99D-86E6B8DAB342");

                    // Note: in a real world application, this encryption key should be
                    // stored in a safe place (e.g in Azure KeyVault, stored as a secret).
                    options.AddEncryptionKey(new SymmetricSecurityKey(
                        Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")));

                    // Register the System.Net.Http integration.
                    options.UseSystemNetHttp();

                    // Register the ASP.NET Core host.
                    options.UseAspNetCore();
                });

            builder.Services.AddScoped<IEmailService, EmailService.EmailService>();

            builder.Services.Configure<AuthenticationOptions>(
                    builder.Configuration.GetSection(AuthenticationOptions.Section));
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


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseCors("MyPolicy");
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseMiddleware<AddTokenAsCookieMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
