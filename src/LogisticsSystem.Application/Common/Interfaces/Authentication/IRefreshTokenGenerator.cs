using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Common.Interfaces.Authentication
{
    public interface IRefreshTokenGenerator
    {
        RefreshToken Generate(Guid userId, int expirationDays);
    }
}
