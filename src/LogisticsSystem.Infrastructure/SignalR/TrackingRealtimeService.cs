using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace LogisticsSystem.Infrastructure.SignalR
{
    public sealed class TrackingRealtimeService : ITrackingRealtimeService
    {
        private readonly IHubContext<TrackingHub> _hubContext;
        private readonly ILogger<TrackingRealtimeService> _logger;

        public TrackingRealtimeService(IHubContext<TrackingHub> hubContext, ILogger<TrackingRealtimeService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task LocationUpdatedAsync(
            Guid shipmentId,
            double latitude,
            double longitude,
            DateTime recordedAt,
            CancellationToken cancellationToken = default)
        {
            var groupName = $"Shipment:{shipmentId}";
            _logger.LogInformation("Broadcasting LocationUpdated to group {GroupName} (Lat: {Latitude}, Lng: {Longitude})", groupName, latitude, longitude);

            await _hubContext.Clients
                .Group(groupName)
                .SendAsync("LocationUpdated", new
                {
                    shipmentId,
                    latitude,
                    longitude,
                    recordedAt
                }, cancellationToken);
        }

        public async Task ShipmentStatusChangedAsync(
            Guid shipmentId,
            ShipmentStatus status,
            DateTime changedAt,
            string? notes = null,
            CancellationToken cancellationToken = default)
        {
            var groupName = $"Shipment:{shipmentId}";
            _logger.LogInformation("Broadcasting ShipmentStatusChanged to group {GroupName} (Status: {Status})", groupName, status);

            await _hubContext.Clients
                .Group(groupName)
                .SendAsync("ShipmentStatusChanged", new
                {
                    shipmentId,
                    status = status.ToString(),
                    changedAt,
                    notes
                }, cancellationToken);
        }
    }
}
