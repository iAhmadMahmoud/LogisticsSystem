namespace LogisticsSystem.Application.Common.Interfaces.Services
{
    public interface IAssignmentExpirationService
    {
        Task ExpireAssignmentsAsync(CancellationToken cancellationToken = default);
    }
}
