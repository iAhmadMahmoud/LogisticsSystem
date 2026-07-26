using LogisticsSystem.Application.Common.Interfaces.Authentication;
using System.Security.Cryptography;

namespace LogisticsSystem.Infrastructure.Authentication.Tokens
{
    public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
    {
        public string GenerateToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);

            return Convert.ToBase64String(randomBytes);
        }
    }
}
