using LogisticsSystem.Application.Features.Dashboard.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.Dashboard.Queries.GetDriverDashboardMetrics
{
    public sealed record GetDriverDashboardMetricsQuery : IRequest<DriverDashboardMetricsDto>;
}
