using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Dashboard.DTOs;
using LogisticsSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Application.Features.Dashboard.Queries.GetShipmentDashboardMetrics
{
    public sealed class GetShipmentDashboardMetricsQueryHandler : IRequestHandler<GetShipmentDashboardMetricsQuery, ShipmentDashboardMetricsDto>
    {
        private readonly IApplicationDbContext _context;

        public GetShipmentDashboardMetricsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ShipmentDashboardMetricsDto> Handle(GetShipmentDashboardMetricsQuery request, CancellationToken cancellationToken)
        {
            var todayUtc = DateTime.UtcNow.Date;
            var tomorrowUtc = todayUtc.AddDays(1);

            var metrics = await _context.Shipments
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(g => new ShipmentDashboardMetricsDto
                {
                    TotalShipments = g.Count(),
                    PendingShipments = g.Count(s => s.Status == ShipmentStatus.Pending),
                    AssignedShipments = g.Count(s => s.Status == ShipmentStatus.Assigned),
                    PickedUpShipments = g.Count(s => s.Status == ShipmentStatus.PickedUp),
                    InTransitShipments = g.Count(s => s.Status == ShipmentStatus.InTransit),
                    DeliveredShipments = g.Count(s => s.Status == ShipmentStatus.Delivered),
                    CancelledShipments = g.Count(s => s.Status == ShipmentStatus.Cancelled),
                    FailedShipments = g.Count(s => s.Status == ShipmentStatus.Failed),
                    ShipmentsCreatedToday = g.Count(s => s.CreatedAt >= todayUtc && s.CreatedAt < tomorrowUtc),
                    ShipmentsDeliveredToday = g.Count(s => s.DeliveredAt.HasValue && s.DeliveredAt.Value >= todayUtc && s.DeliveredAt.Value < tomorrowUtc)
                })
                .FirstOrDefaultAsync(cancellationToken);

            return metrics ?? new ShipmentDashboardMetricsDto();
        }
    }
}
