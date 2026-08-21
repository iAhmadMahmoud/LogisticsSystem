using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Dashboard.DTOs;
using LogisticsSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Application.Features.Dashboard.Queries.GetDriverDashboardMetrics
{
    public sealed class GetDriverDashboardMetricsQueryHandler : IRequestHandler<GetDriverDashboardMetricsQuery, DriverDashboardMetricsDto>
    {
        private readonly IApplicationDbContext _context;

        public GetDriverDashboardMetricsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DriverDashboardMetricsDto> Handle(GetDriverDashboardMetricsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Drivers.AsNoTracking();

            var statusCounts = await query
                .GroupBy(d => d.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

            var totalDrivers = statusCounts.Values.Sum();
            var availableDrivers = statusCounts.GetValueOrDefault(DriverStatus.Available, 0);
            var busyDrivers = statusCounts.GetValueOrDefault(DriverStatus.Busy, 0);
            var offlineDrivers = statusCounts.GetValueOrDefault(DriverStatus.Offline, 0);
            var onBreakDrivers = statusCounts.GetValueOrDefault(DriverStatus.OnBreak, 0);
            var suspendedDrivers = statusCounts.GetValueOrDefault(DriverStatus.Suspended, 0);

            var withVehicles = await query.CountAsync(d => d.VehicleId != null, cancellationToken);
            var withoutVehicles = totalDrivers - withVehicles;

            var activeDrivers = totalDrivers - suspendedDrivers;
            var inactiveDrivers = suspendedDrivers;

            return new DriverDashboardMetricsDto
            {
                TotalDrivers = totalDrivers,
                AvailableDrivers = availableDrivers,
                BusyDrivers = busyDrivers,
                OfflineDrivers = offlineDrivers,
                OnBreakDrivers = onBreakDrivers,
                SuspendedDrivers = suspendedDrivers,
                DriversWithVehicles = withVehicles,
                DriversWithoutVehicles = withoutVehicles,
                ActiveDrivers = activeDrivers,
                InactiveDrivers = inactiveDrivers
            };
        }
    }
}
