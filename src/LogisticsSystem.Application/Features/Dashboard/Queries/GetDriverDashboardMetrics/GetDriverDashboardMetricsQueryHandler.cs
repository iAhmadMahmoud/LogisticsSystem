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

            var totalDrivers = await query.CountAsync(cancellationToken);
            var availableDrivers = await query.CountAsync(d => d.Status == DriverStatus.Available, cancellationToken);
            var busyDrivers = await query.CountAsync(d => d.Status == DriverStatus.Busy, cancellationToken);
            var offlineDrivers = await query.CountAsync(d => d.Status == DriverStatus.Offline, cancellationToken);
            var onBreakDrivers = await query.CountAsync(d => d.Status == DriverStatus.OnBreak, cancellationToken);
            var suspendedDrivers = await query.CountAsync(d => d.Status == DriverStatus.Suspended, cancellationToken);

            var withVehicles = await query.CountAsync(d => d.VehicleId != null, cancellationToken);
            var withoutVehicles = await query.CountAsync(d => d.VehicleId == null, cancellationToken);

            var activeDrivers = await query.CountAsync(d => d.Status != DriverStatus.Suspended, cancellationToken);
            var inactiveDrivers = await query.CountAsync(d => d.Status == DriverStatus.Suspended, cancellationToken);

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
