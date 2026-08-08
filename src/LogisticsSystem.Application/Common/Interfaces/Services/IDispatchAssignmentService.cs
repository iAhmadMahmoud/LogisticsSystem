using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Common.Interfaces.Services
{
    public interface IDispatchAssignmentService
    {
        Task<DispatchAssignment?> CreateAssignmentAsync(Shipment shipment, Driver driver, CancellationToken cancellationToken = default);
    }
}
