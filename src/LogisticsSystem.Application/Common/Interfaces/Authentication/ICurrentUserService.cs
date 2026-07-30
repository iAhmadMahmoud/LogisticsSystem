namespace LogisticsSystem.Application.Common.Interfaces.Authentication
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }

        bool IsInRole(string role);
    }
}
