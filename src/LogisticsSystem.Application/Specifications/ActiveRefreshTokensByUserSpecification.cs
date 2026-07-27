using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Specifications
{
    public sealed class ActiveRefreshTokensByUserSpecification : BaseSpecification<RefreshToken>
    {
        public ActiveRefreshTokensByUserSpecification(Guid userId)
             : base(x => x.UserId == userId &&
           !x.IsRevoked &&
           x.ExpiresAt > DateTime.UtcNow)
        { }
    }
}