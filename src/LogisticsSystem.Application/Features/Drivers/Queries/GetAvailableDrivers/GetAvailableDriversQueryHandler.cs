using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Drivers.Specifications;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Drivers.Queries.GetAvailableDrivers
{
    public sealed class GetAvailableDriversQueryHandler : IRequestHandler<GetAvailableDriversQuery, IReadOnlyList<DriverResponse>>
    {
        private readonly IGenericRepository<Driver> _driverRepository;

        public GetAvailableDriversQueryHandler(IGenericRepository<Driver> driverRepository)
        {
            _driverRepository = driverRepository;
        }

        public async Task<IReadOnlyList<DriverResponse>> Handle(GetAvailableDriversQuery request, CancellationToken cancellationToken)
        {
            var driver = await _driverRepository.ListAsync(new AvailableDriversSpecification());

            return driver.Select(d=> new DriverResponse
            {
                Id = d.Id,
                UserId = d.UserId,
                LicenseNumber = d.LicenseNumber,
                Latitude = d.Latitude,
                Longitude = d.Longitude
            }).ToList();
        }
    }
}
