using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Hangfire.Dashboard;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Infrastructure.Authentication.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LogisticsSystem.Infrastructure.BackgroundJobs
{
    public sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private const string CookieName = "hangfire_token";

        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            if (httpContext is null)
            {
                return false;
            }

            // 1. Check if caller is already authenticated and in Admin role (e.g. via Bearer header)
            if (httpContext.User.Identity?.IsAuthenticated == true &&
                httpContext.User.IsInRole(Roles.Admin))
            {
                return true;
            }

            // 2. Allow passing JWT token via query string or persisted cookie
            var queryToken = httpContext.Request.Query["access_token"].FirstOrDefault()
                             ?? httpContext.Request.Query["jwt"].FirstOrDefault();

            var token = queryToken ?? httpContext.Request.Cookies[CookieName];

            if (!string.IsNullOrWhiteSpace(token))
            {
                var jwtOptions = httpContext.RequestServices
                    .GetService<IOptions<JwtOptions>>()?.Value;

                if (jwtOptions != null && !string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
                {
                    try
                    {
                        var tokenHandler = new JwtSecurityTokenHandler();
                        var key = Encoding.UTF8.GetBytes(jwtOptions.SecretKey);
                        var validationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = jwtOptions.Issuer,
                            ValidAudience = jwtOptions.Audience,
                            IssuerSigningKey = new SymmetricSecurityKey(key),
                            RoleClaimType = ClaimTypes.Role,
                            ClockSkew = TimeSpan.Zero
                        };

                        var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                        if (principal.IsInRole(Roles.Admin))
                        {
                            if (!string.IsNullOrWhiteSpace(queryToken))
                            {
                                httpContext.Response.Cookies.Append(CookieName, queryToken, new CookieOptions
                                {
                                    HttpOnly = true,
                                    Secure = httpContext.Request.IsHttps,
                                    SameSite = SameSiteMode.Lax,
                                    Expires = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.AccessTokenExpirationMinutes)
                                });
                            }

                            return true;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            return false;
        }
    }
}
