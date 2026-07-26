using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Domain.Entities;
using System.Security.Cryptography;

namespace LogisticsSystem.Infrastructure.Authentication.Tokens
{
    public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
    {
        public RefreshToken Generate(Guid userId, int expirationDays)
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            var tokenString = Convert.ToBase64String(randomBytes);

            return new RefreshToken
            {
                UserId = userId,
                Token = tokenString,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
                IsRevoked = false
            };
        }
    }
}
