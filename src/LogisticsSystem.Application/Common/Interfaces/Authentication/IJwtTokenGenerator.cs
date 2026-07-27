using LogisticsSystem.Application.Common.Models.Authentication;

namespace LogisticsSystem.Application.Common.Interfaces.Authentication
{
    public interface IJwtTokenGenerator
    {
        Task<string> GenerateAccessTokenAsync(JwtUser user);
    }
}
