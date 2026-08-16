using Hangfire;
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
            services.AddDbContext<ApplicationDbContext>(option =>
            {
                option.UseSqlServer(configuration.GetConnectionString("LogisticsSystem"));
            });

            services.AddHangfire(config =>
            {
                config.UseSqlServerStorage(
                    configuration.GetConnectionString("LogisticsSystem"));
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
                    var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwt.Issuer,
                        ValidAudience = jwt.Audience,

                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwt.SecretKey)),

                        NameClaimType = ClaimTypes.NameIdentifier,
                        RoleClaimType = ClaimTypes.Role,

                        ClockSkew = TimeSpan.Zero
                    };

                    // ── TEMPORARY DIAGNOSTICS ── remove after root cause is confirmed ──
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];

                            var path = context.HttpContext.Request.Path;

                            if (!string.IsNullOrEmpty(accessToken) &&
                                path.StartsWithSegments("/hubs/notifications"))
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("JwtDiagnostics");
                            logger.LogError(
                                "[JwtDiagnostics] Authentication failed: {ExceptionType}: {Message}",
                                context.Exception.GetType().Name,
                                context.Exception.Message);
                            return Task.CompletedTask;
                        },
                        OnChallenge = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("JwtDiagnostics");
                            logger.LogWarning(
                                "[JwtDiagnostics] OnChallenge fired — Error: {Error}, ErrorDescription: {ErrorDescription}",
                                context.Error,
                                context.ErrorDescription);
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("JwtDiagnostics");
                            logger.LogInformation(
                                "[JwtDiagnostics] Token validated successfully for principal: {Name}",
                                context.Principal?.Identity?.Name ?? "(unknown)");
                            return Task.CompletedTask;
                        }
                    };
                    // ── END TEMPORARY DIAGNOSTICS ──
                });

            services.AddApplicationAuthorization();

            services.AddScoped<Persistence.Seed.DbInitializer>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IShipmentStatusHistoryService,ShipmentStatusHistoryService>();
            services.AddScoped<IDriverAssignmentService, DriverAssignmentService>();
            services.AddScoped<IAssignmentExpirationService, AssignmentExpirationService>();

            services.AddScoped<IDispatchAssignmentService, DispatchAssignmentService>();
            services.AddScoped<IShipmentAssignmentScheduler, ShipmentAssignmentScheduler>();
            services.AddScoped<ShipmentAssignmentJob>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<INotificationRealtimeService, NotificationRealtimeService>();
            services.AddSingleton<IUserIdProvider, UserIdProvider>();

            return services;
        }
    }
}
