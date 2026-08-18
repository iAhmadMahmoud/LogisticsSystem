using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Drivers.Specifications;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Drivers.Queries.GetAllDrivers
{
    public sealed class GetAllDriversQueryHandler : IRequestHandler<GetAllDriversQuery,PagedResult<DriverListItemResponse>>
    {
        private readonly IGenericRepository<Driver> _driverRepository;

        public GetAllDriversQueryHandler(IGenericRepository<Driver> driverRepository)
        {
            _driverRepository = driverRepository;
        }

        async Task<PagedResult<DriverListItemResponse>> IRequestHandler<GetAllDriversQuery, PagedResult<DriverListItemResponse>>.Handle(GetAllDriversQuery request, CancellationToken cancellationToken)
        {
            var specification = new GetAllDriversSpecification(request.PageNumber,request.PageSize,request.Status);

            var drivers = await _driverRepository.ListAsync(specification, cancellationToken);

            var totalCount = await _driverRepository.CountAsync(specification, cancellationToken);

            var items = drivers.Select(driver => new DriverListItemResponse
            {
                Id = driver.Id,
                UserId = driver.UserId,
                LicenseNumber = driver.LicenseNumber,
                Status = driver.Status,
                Latitude = driver.Latitude,
                Longitude = driver.Longitude,
                VehicleId = driver.VehicleId
            }).ToList();

            return new PagedResult<DriverListItemResponse>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount,
            };

        }
    }
}
