using Volo.Authentication.OpenIddict.Database;
using Volo.Authentication.OpenIddict.HostedServices;
using Volo.Authentication.OpenIddict.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;

namespace Volo.Authentication.OpenIddict
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddCors();
            builder.Services.AddControllers();

            builder.Services.AddLogging(o => o.AddDebug());

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                {
                    // Configure Entity Framework Core to use Microsoft SQL Server.
                    options.UseSqlServer(connectionString);
                    // Register the entity sets needed by OpenIddict.
                    // Note: use the generic overload if you need to replace the default OpenIddict entities.
                    options.UseOpenIddict();
                });

            builder.Services
                .AddIdentity<IdentityUser, IdentityRole>(o =>
                {
                    o.Lockout.MaxFailedAccessAttempts = 2;
                    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    o.Lockout.AllowedForNewUsers = true;

                    o.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            builder.Services.Configure<DataProtectionTokenProviderOptions>(opt =>
                    opt.TokenLifespan = TimeSpan.FromHours(2));

            // OpenIddict offers native integration with Quartz.NET to perform scheduled tasks
            // (like pruning orphaned authorizations/tokens from the database) at regular intervals.
            builder.Services.AddQuartz(options =>
            {
                options.UseMicrosoftDependencyInjectionJobFactory();
                options.UseSimpleTypeLoader();
                options.UseInMemoryStore();
            });

            // Register the Quartz.NET service and configure it to block shutdown until jobs are complete.
            builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

            builder.Services.AddOpenIddict()
                // Register the OpenIddict core components.
                .AddCore(options => 
                {
                    // Configure OpenIddict to use the Entity Framework Core stores and models.
                    // Note: call ReplaceDefaultEntities() to replace the default entities.
                    options.UseEntityFrameworkCore()
                        .UseDbContext<ApplicationDbContext>();

                    // Enable Quartz.NET integration.
                    options.UseQuartz();
                })
                // Register the OpenIddict server components.
                .AddServer(options =>
                {
                    //specify token endpoint uri
                    options.SetTokenEndpointUris("connect/token")
                           .SetIntrospectionEndpointUris("connect/introspect");

                    // Add all auth flows you want to support
                    options.AllowPasswordFlow()
                           .AllowRefreshTokenFlow()
                           .SetAccessTokenLifetime(TimeSpan.FromMinutes(30))
                           .SetRefreshTokenLifetime(TimeSpan.FromDays(1));

                    options.DisableSlidingRefreshTokenExpiration();

                    // Custom auth flows are also supported

                    //options.AllowCustomFlow("custom_flow_name");


                    // Accept anonymous clients (i.e clients that don't send a client_id).
                    //options.AcceptAnonymousClients();

                    // Note: in a real world application, this encryption key should be
                    // stored in a safe place (e.g in Azure KeyVault, stored as a secret).
                    options.AddEncryptionKey(new SymmetricSecurityKey(
                        Convert.FromBase64String("DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=")));


                    // Using reference tokens means the actual access and refresh tokens
                    // are stored in the database and different tokens, referencing the actual
                    // tokens (in the db), are used in request headers. The actual tokens are not
                    // made public.

                    //options.UseReferenceAccessTokens();
                    //options.UseReferenceRefreshTokens();



                    // Register the signing and encryption credentials.
                    //options.AddDevelopmentEncryptionCertificate();
                    //options.AddDevelopmentSigningCertificate();
                    options.AddEphemeralSigningKey();


                    // Disables access token body encryption
                    options.DisableAccessTokenEncryption();

                    // Register the ASP.NET Core host and configure the ASP.NET Core options.
                    options.UseAspNetCore()
                    .EnableTokenEndpointPassthrough()
                    .DisableTransportSecurityRequirement(); // During development, you can disable the HTTPS requirement.;
                })
                .AddValidation(options =>
                {
                    // Import the configuration from the local OpenIddict server instance.
                    options.UseLocalServer();

                    // Register the ASP.NET Core host.
                    options.UseAspNetCore();
                });


            builder.Services.AddCors();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddHostedService<ClientSeeder>();

            builder.Services.AddAuthorization();

            var app = builder.Build();

            //seed user and role
            using (var scope = app.Services.CreateScope())
            {
                using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.MigrateAsync();

                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                

                foreach (var role in Role.AllRoles)
                {
                    if (!await roleManager.RoleExistsAsync(role.Name.ToUpper()))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role.Name));
                    }
                }

                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                string adminEmail = "admin@admin.com";
                string password = "VoloTest!!123456";
                if ((await userManager.FindByNameAsync(adminEmail)) == null)
                {
                    var user = new IdentityUser();
                    user.UserName = adminEmail;
                    user.Email = adminEmail;

                    await userManager.CreateAsync(user, password);

                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }

            // Configure the HTTP request pipeline.

            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseCors(o => o.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());
            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
