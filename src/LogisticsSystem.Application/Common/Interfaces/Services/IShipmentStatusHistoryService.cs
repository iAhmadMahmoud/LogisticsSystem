using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Application.Common.Interfaces.Services
{
    public interface IShipmentStatusHistoryService
    {
        Task AddAsync(Shipment shipment, ShipmentStatus status, Guid? changedByUserId, CancellationToken cancellationToken = default);
    }
}
