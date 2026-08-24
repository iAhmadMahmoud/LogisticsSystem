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
            var metrics = await _context.Drivers
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalDrivers = g.Count(),
                    AvailableDrivers = g.Count(d => d.Status == DriverStatus.Available),
                    BusyDrivers = g.Count(d => d.Status == DriverStatus.Busy),
                    OfflineDrivers = g.Count(d => d.Status == DriverStatus.Offline),
                    OnBreakDrivers = g.Count(d => d.Status == DriverStatus.OnBreak),
                    SuspendedDrivers = g.Count(d => d.Status == DriverStatus.Suspended),
                    DriversWithVehicles = g.Count(d => d.VehicleId != null)
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (metrics is null)
            {
                return new DriverDashboardMetricsDto();
            }

            var withoutVehicles = metrics.TotalDrivers - metrics.DriversWithVehicles;
            var activeDrivers = metrics.TotalDrivers - metrics.SuspendedDrivers;
            var inactiveDrivers = metrics.SuspendedDrivers;

            return new DriverDashboardMetricsDto
            {
                TotalDrivers = metrics.TotalDrivers,
                AvailableDrivers = metrics.AvailableDrivers,
                BusyDrivers = metrics.BusyDrivers,
                OfflineDrivers = metrics.OfflineDrivers,
                OnBreakDrivers = metrics.OnBreakDrivers,
                SuspendedDrivers = metrics.SuspendedDrivers,
                DriversWithVehicles = metrics.DriversWithVehicles,
                DriversWithoutVehicles = withoutVehicles,
                ActiveDrivers = activeDrivers,
                InactiveDrivers = inactiveDrivers
            };
        }
    }
}
