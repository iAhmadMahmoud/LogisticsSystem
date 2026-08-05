using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Drivers.Specifications;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Drivers.Queries.GetDriverById
{
    public sealed class GetDriverByIdQueryHandler : IRequestHandler<GetDriverByIdQuery, DriverDetailsResponse>
    {
        private readonly IGenericRepository<Driver> _driverRepository;

        public GetDriverByIdQueryHandler(IGenericRepository<Driver> driverRepository)
        {
            _driverRepository = driverRepository;
        }

        public async Task<DriverDetailsResponse> Handle(GetDriverByIdQuery request, CancellationToken cancellationToken)
        {
            var specification = new DriverByIdSpecification(request.DriverId);

            var driver = await _driverRepository.FirstOrDefaultAsync(specification, cancellationToken);

            if(driver is null)
            {
                throw new KeyNotFoundException("Driver not found.");
            }

            return new DriverDetailsResponse
            {
                Id = driver.Id,
                UserId = driver.UserId,
                LicenseNumber = driver.LicenseNumber,
                Status = driver.Status,
                Latitude = driver.Latitude,
                Longitude = driver.Longitude,
                VehicleId = driver.VehicleId
            };
        }
    }
}
