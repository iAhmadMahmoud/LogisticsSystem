using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Drivers.Specifications;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Infrastructure.Services
{
    public sealed class DriverAssignmentService : IDriverAssignmentService
    {
        private readonly IGenericRepository<Driver> _driverRepository;

        public DriverAssignmentService(IGenericRepository<Driver> driverRepository)
        {
            _driverRepository = driverRepository;
        }

        public async Task<Driver?> FindBestAvailableDriverAsync(double pickupLatitude, double pickupLongitude, CancellationToken cancellationToken = default)
        {
            var drivers = await _driverRepository.ListAsync(new AvailableDriversSpecification(),cancellationToken);

            var candidates = drivers.Where(d => d.Latitude.HasValue && d.Longitude.HasValue)
                .Select(d => new
                {
                    Driver = d,
                    Distance = CalculateDistance(pickupLatitude, pickupLongitude, d.Latitude!.Value, d.Longitude!.Value)
                }).OrderBy(d => d.Distance)
                .ToList();

            return candidates.FirstOrDefault()?.Driver;
        }

        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
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

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }
        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}
