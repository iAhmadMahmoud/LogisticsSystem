using Hangfire;
using Hangfire.SqlServer;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Infrastructure.Authentication.Authorization;
using LogisticsSystem.Infrastructure.Authentication.Email;
using LogisticsSystem.Infrastructure.Authentication.Identity;
using LogisticsSystem.Infrastructure.Authentication.Jwt;
using LogisticsSystem.Infrastructure.Authentication.Tokens;
using LogisticsSystem.Infrastructure.BackgroundJobs;
using LogisticsSystem.Infrastructure.Identity;
using LogisticsSystem.Infrastructure.Persistence;
using LogisticsSystem.Infrastructure.Persistence.Interceptors;
using LogisticsSystem.Infrastructure.Persistence.Repositories;
using LogisticsSystem.Infrastructure.Services;
using LogisticsSystem.Infrastructure.SignalR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace LogisticsSystem.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure
            (
            this IServiceCollection services,
            IConfiguration configuration
            )
        {
            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(configuration.GetConnectionString("LogisticsSystem"));

                options.AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>());

            });

            services.AddHangfire(config =>
            {
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                      .UseSimpleAssemblyNameTypeSerializer()
                      .UseRecommendedSerializerSettings()
                      .UseSqlServerStorage(
                          configuration.GetConnectionString("LogisticsSystem"),
                          new SqlServerStorageOptions
                          {
                              CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                              SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                              QueuePollInterval = TimeSpan.FromSeconds(15),
                              UseRecommendedIsolationLevel = true,
                              DisableGlobalLocks = true
                          });
            });

            services.AddSignalR();



            services.AddHangfireServer();

            services.AddScoped<IApplicationDbContext>(sp =>
                sp.GetRequiredService<ApplicationDbContext>());


            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            services.AddScoped<IUnitOfWork, UnitOfWork>();





            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.User.RequireUniqueEmail = true;

                options.SignIn.RequireConfirmedEmail = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

            services.AddHttpContextAccessor();

            services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
            services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
            services.Configure<DispatchOptions>(configuration.GetSection("Dispatch"));

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
                    var secretKey = !string.IsNullOrWhiteSpace(jwt.SecretKey)
                        ? jwt.SecretKey
                        : "TemporaryFallbackSecretKeyForConfigurationBindingVerification1234567890!";

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwt.Issuer,
                        ValidAudience = jwt.Audience,

                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(secretKey)),

                        NameClaimType = ClaimTypes.NameIdentifier,
                        RoleClaimType = ClaimTypes.Role,

                        ClockSkew = TimeSpan.Zero
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            if (!string.IsNullOrEmpty(accessToken) &&
                                path.StartsWithSegments("/hubs"))
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddApplicationAuthorization();

            services.AddScoped<Persistence.Seed.DbInitializer>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<EmailSender>();
            services.AddScoped<FakeEmailSender>();
            services.AddScoped<SmtpEmailSender>();
            services.AddScoped<IEmailSender>(sp =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailOptions>>().Value;
                if (string.Equals(options.Provider, "Smtp", StringComparison.OrdinalIgnoreCase))
                {
                    return sp.GetRequiredService<SmtpEmailSender>();
                }
                return sp.GetRequiredService<FakeEmailSender>();
            });
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IShipmentStatusHistoryService,ShipmentStatusHistoryService>();
            services.AddScoped<IDriverAssignmentService, DriverAssignmentService>();
            services.AddScoped<IAssignmentExpirationService, AssignmentExpirationService>();

            services.AddScoped<IDispatchAssignmentService, DispatchAssignmentService>();
            services.AddScoped<IShipmentAssignmentScheduler, ShipmentAssignmentScheduler>();
            services.AddScoped<ShipmentAssignmentJob>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<INotificationRealtimeService, NotificationRealtimeService>();
            services.AddScoped<ITrackingRealtimeService, TrackingRealtimeService>();
            services.AddSingleton<IUserIdProvider, UserIdProvider>();

            services.AddScoped<AuditSaveChangesInterceptor>();

            return services;
        }
    }
}
