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

            var totalShipments = await query.CountAsync(cancellationToken);
            var pendingShipments = await query.CountAsync(s => s.Status == ShipmentStatus.Pending, cancellationToken);
            var assignedShipments = await query.CountAsync(s => s.Status == ShipmentStatus.Assigned, cancellationToken);
            var pickedUpShipments = await query.CountAsync(s => s.Status == ShipmentStatus.PickedUp, cancellationToken);
            var inTransitShipments = await query.CountAsync(s => s.Status == ShipmentStatus.InTransit, cancellationToken);
            var deliveredShipments = await query.CountAsync(s => s.Status == ShipmentStatus.Delivered, cancellationToken);
            var cancelledShipments = await query.CountAsync(s => s.Status == ShipmentStatus.Cancelled, cancellationToken);
            var failedShipments = await query.CountAsync(s => s.Status == ShipmentStatus.Failed, cancellationToken);

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
