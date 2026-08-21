using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Dashboard.DTOs;
using LogisticsSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Application.Features.Dashboard.Queries.GetRecentActivity
{
    public sealed class GetRecentActivityQueryHandler : IRequestHandler<GetRecentActivityQuery, PagedResult<RecentActivityDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetRecentActivityQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<RecentActivityDto>> Handle(GetRecentActivityQuery request, CancellationToken cancellationToken)
        {
            var query = _context.ShipmentStatusHistories.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.ActivityType))
            {
                var filter = request.ActivityType.Trim();
                if (Enum.TryParse<ShipmentStatus>(filter, true, out var status))
                {
                    query = query.Where(h => h.Status == status);
                }
                else
                {
                    var matchingStatuses = Enum.GetValues<ShipmentStatus>()
                        .Where(s => FormatActivityType(s).Equals(filter, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (matchingStatuses.Count > 0)
                    {
                        query = query.Where(h => matchingStatuses.Contains(h.Status));
                    }
                    else
                    {
                        return new PagedResult<RecentActivityDto>
                        {
                            Items = [],
                            TotalCount = 0,
                            PageNumber = request.PageNumber,
                            PageSize = request.PageSize
                        };
                    }
                }
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var projectedItems = await query
                .OrderByDescending(h => h.ChangedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(h => new
                {
                    h.Id,
                    h.Status,
                    TrackingNumber = h.Shipment != null ? h.Shipment.TrackingNumber : string.Empty,
                    h.ShipmentId,
                    h.ChangedAt,
                    h.ChangedByUserId
                })
                .ToListAsync(cancellationToken);

            var dtos = projectedItems.Select(h => new RecentActivityDto
            {
                Id = h.Id,
                ActivityType = FormatActivityType(h.Status),
                Description = FormatDescription(h.Status, h.TrackingNumber),
                EntityId = h.ShipmentId,
                EntityType = "Shipment",
                Timestamp = h.ChangedAt,
                UserId = h.ChangedByUserId
            }).ToList();

            return new PagedResult<RecentActivityDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        public static string FormatActivityType(ShipmentStatus status) => status switch
        {
            ShipmentStatus.Pending => "ShipmentCreated",
            ShipmentStatus.Assigned => "ShipmentAssigned",
            ShipmentStatus.PickedUp => "ShipmentPickedUp",
            ShipmentStatus.InTransit => "ShipmentInTransit",
            ShipmentStatus.Delivered => "ShipmentDelivered",
            ShipmentStatus.Cancelled => "ShipmentCancelled",
            ShipmentStatus.Failed => "ShipmentFailed",
            _ => status.ToString()
        };

        public static string FormatDescription(ShipmentStatus status, string trackingNumber) => status switch
        {
            ShipmentStatus.Pending => $"Shipment {trackingNumber} was created.",
            ShipmentStatus.Assigned => $"Shipment {trackingNumber} was assigned to a driver.",
            ShipmentStatus.PickedUp => $"Shipment {trackingNumber} was picked up.",
            ShipmentStatus.InTransit => $"Shipment {trackingNumber} entered transit.",
            ShipmentStatus.Delivered => $"Shipment {trackingNumber} was delivered successfully.",
            ShipmentStatus.Cancelled => $"Shipment {trackingNumber} was cancelled.",
            ShipmentStatus.Failed => $"Shipment {trackingNumber} delivery failed.",
            _ => $"Shipment {trackingNumber} status changed to {status}."
        };
    }
}
