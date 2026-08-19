using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Application.Common.Interfaces.Services
{
    public interface ITrackingRealtimeService
    {
        Task LocationUpdatedAsync(
            Guid shipmentId,
            double latitude,
            double longitude,
            DateTime recordedAt,
            CancellationToken cancellationToken = default);

        Task ShipmentStatusChangedAsync(
            Guid shipmentId,
            ShipmentStatus status,
            DateTime changedAt,
            string? notes = null,
            CancellationToken cancellationToken = default);
    }
}
