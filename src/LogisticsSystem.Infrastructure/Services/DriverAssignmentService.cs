using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Drivers.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogisticsSystem.Infrastructure.Services
{
    public sealed class DriverAssignmentService : IDriverAssignmentService
    {
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly IGenericRepository<DispatchAssignment> _assignmentRepository;
        private readonly ILogger<DriverAssignmentService> _logger;

        public DriverAssignmentService(
            IGenericRepository<Driver> driverRepository,
            IGenericRepository<DispatchAssignment> assignmentRepository,
            ILogger<DriverAssignmentService> logger)
        {
            _driverRepository = driverRepository;
            _assignmentRepository = assignmentRepository;
            _logger = logger;
        }

        public async Task<Driver?> FindBestAvailableDriverAsync(
            Shipment shipment,
            CancellationToken cancellationToken = default)
        {
            // 1. Get all available drivers
            var drivers = await _driverRepository.ListAsync(
                new AvailableDriversSpecification(),
                cancellationToken);

            // 2. Get drivers who already received an assignment for this shipment
            var assignedDriverIds = await _assignmentRepository
                .AsQueryable()
                .Where(x => x.ShipmentId == shipment.Id)
                .Select(x => x.DriverId)
                .Distinct()
                .ToListAsync(cancellationToken);

            // 3. Remove drivers who already have an assignment for this shipment
            var candidates = drivers
                .Where(d =>
                    d.Latitude.HasValue &&
                    d.Longitude.HasValue &&
                    !assignedDriverIds.Contains(d.Id))
                .Select(d => new
                {
                    Driver = d,
                    Distance = CalculateDistance(
                        shipment.PickupLatitude,
                        shipment.PickupLongitude,
                        d.Latitude!.Value,
                        d.Longitude!.Value)
                })
                .OrderBy(x => x.Distance)
                .ToList();

            var selected = candidates.FirstOrDefault();

            if (selected is not null)
            {
                _logger.LogInformation(
                    "Dispatch: Selected driver {DriverId} (distance: {DistanceKm:F2} km) for shipment {ShipmentId} from {CandidateCount} candidates.",
                    selected.Driver.Id,
                    selected.Distance,
                    shipment.Id,
                    candidates.Count);
            }
            else
            {
                _logger.LogWarning(
                    "Dispatch: No available eligible driver found for shipment {ShipmentId} ({TotalAvailable} available drivers disqualified).",
                    shipment.Id,
                    drivers.Count);
            }

            // 4. Return nearest eligible driver
            return selected?.Driver;
        }

        private static double CalculateDistance(
            double lat1,
            double lon1,
            double lat2,
            double lon2)
        {
            const double earthRadiusKm = 6371;

            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) *
                Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) *
                Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(
                Math.Sqrt(a),
                Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}