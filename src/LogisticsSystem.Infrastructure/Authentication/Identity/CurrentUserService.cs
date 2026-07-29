using LogisticsSystem.Application.Common.Interfaces.Authentication;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LogisticsSystem.Infrastructure.Authentication.Identity
{
    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid UserId
        {
            get
            {
                var userIdValue =
                    _httpContextAccessor.HttpContext?
                        .User
                        .FindFirstValue(ClaimTypes.NameIdentifier)
                    ??
                    _httpContextAccessor.HttpContext?
                        .User
                        .FindFirstValue("sub");

                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    throw new UnauthorizedAccessException(
                        "Authenticated user ID is missing or invalid.");
                }

                return userId;
            }
        }
    }
}
