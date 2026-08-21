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

            var query = _context.Shipments.AsNoTracking();

            var statusCounts = await query
                .GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

            var totalShipments = statusCounts.Values.Sum();
            var pendingShipments = statusCounts.GetValueOrDefault(ShipmentStatus.Pending, 0);
            var assignedShipments = statusCounts.GetValueOrDefault(ShipmentStatus.Assigned, 0);
            var pickedUpShipments = statusCounts.GetValueOrDefault(ShipmentStatus.PickedUp, 0);
            var inTransitShipments = statusCounts.GetValueOrDefault(ShipmentStatus.InTransit, 0);
            var deliveredShipments = statusCounts.GetValueOrDefault(ShipmentStatus.Delivered, 0);
            var cancelledShipments = statusCounts.GetValueOrDefault(ShipmentStatus.Cancelled, 0);
            var failedShipments = statusCounts.GetValueOrDefault(ShipmentStatus.Failed, 0);

            var createdToday = await query.CountAsync(s => s.CreatedAt >= todayUtc && s.CreatedAt < tomorrowUtc, cancellationToken);
            var deliveredToday = await query.CountAsync(s => s.DeliveredAt.HasValue && s.DeliveredAt.Value >= todayUtc && s.DeliveredAt.Value < tomorrowUtc, cancellationToken);

            return new ShipmentDashboardMetricsDto
            {
                TotalShipments = totalShipments,
                PendingShipments = pendingShipments,
                AssignedShipments = assignedShipments,
                PickedUpShipments = pickedUpShipments,
                InTransitShipments = inTransitShipments,
                DeliveredShipments = deliveredShipments,
                CancelledShipments = cancelledShipments,
                FailedShipments = failedShipments,
                ShipmentsCreatedToday = createdToday,
                ShipmentsDeliveredToday = deliveredToday
            };
        }
    }
}
