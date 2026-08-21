using LogisticsSystem.Application.Features.Dashboard.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.Dashboard.Queries.GetShipmentDashboardMetrics
{
    public sealed record GetShipmentDashboardMetricsQuery : IRequest<ShipmentDashboardMetricsDto>;
}
