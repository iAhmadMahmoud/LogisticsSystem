using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Drivers.Specifications;
using LogisticsSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Infrastructure.Services
{
    public sealed class NearestDriverService : INearestDriverService
    {
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly IGenericRepository<DispatchAssignment> _assignmentRepository;

        public NearestDriverService(IGenericRepository<Driver> driverRepository, IGenericRepository<DispatchAssignment> assignmentRepository)
        {
            _driverRepository = driverRepository;
            _assignmentRepository = assignmentRepository;
        }

        public async Task<NearestDriverResult?> FindNerstAsync(Shipment shipment, CancellationToken cancellationToken = default)
        {
            var specification = new AvailableDriversSpecification();

            var drivers = await _driverRepository.ListAsync(specification, cancellationToken);

            if (drivers.Count == 0)
                return null;

            var assignedDriverIds = await _assignmentRepository
                .AsQueryable()
                .Where(x => x.ShipmentId == shipment.Id)
                .Select(x => x.DriverId)
                .Distinct()
                .ToListAsync(cancellationToken);


            var nearestDriver = drivers
                .Where(driver =>
                    driver.Latitude.HasValue &&
                    driver.Longitude.HasValue &&
                    !assignedDriverIds.Contains(driver.Id))
                .Select(driver => new NearestDriverResult(
                    driver.Id,
                    CalculateDistanceKm(
                        shipment.PickupLatitude,
                        shipment.PickupLongitude,
                        driver.Latitude.Value,
                        driver.Longitude.Value)))
                .OrderBy(x => x.DistanceKm)
                .FirstOrDefault();

            return nearestDriver;
        }
        private static double CalculateDistanceKm(
            double latitude1,
            double longitude1,
            double latitude2,
            double longitude2)
        {
            const double earthRadiusKm = 6371.0;

            var lat1 = DegreesToRadians(latitude1);
            var lat2 = DegreesToRadians(latitude2);

            var deltaLat = DegreesToRadians(latitude2 - latitude1);
            var deltaLon = DegreesToRadians(longitude2 - longitude1);

            var a =
                Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) *
                Math.Cos(lat2) *
                Math.Sin(deltaLon / 2) *
                Math.Sin(deltaLon / 2);

            var c = 2 * Math.Atan2(
                Math.Sqrt(a),
                Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees)
            => degrees * Math.PI / 180.0;
    }
}
