using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Vehicles.DTOs;
using LogisticsSystem.Application.Features.Vehicles.Specifications;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Vehicles.Queries.GetAvailableVehicles
{
    public sealed class GetAvailableVehiclesQueryHandler : IRequestHandler<GetAvailableVehiclesQuery, PagedResult<VehicleDto>>
    {
        private readonly IGenericRepository<Vehicle> _vehicleRepository;

        public GetAvailableVehiclesQueryHandler(IGenericRepository<Vehicle> vehicleRepository)
        {
            _vehicleRepository = vehicleRepository;
        }

        public async Task<PagedResult<VehicleDto>> Handle(GetAvailableVehiclesQuery request, CancellationToken cancellationToken)
        {
            var countSpecification = new AvailableVehiclesSpecification(request, isPaging: false);
            var totalCount = await _vehicleRepository.CountAsync(countSpecification, cancellationToken);

            var filterSpecification = new AvailableVehiclesSpecification(request, isPaging: true);
            var vehicles = await _vehicleRepository.ListAsync(filterSpecification, cancellationToken);

            var dtos = vehicles.Select(v => new VehicleDto
            {
                Id = v.Id,
                PlateNumber = v.PlateNumber,
                Brand = v.Brand,
                Model = v.Model,
                ManufacturingYear = v.ManufacturingYear,
                Color = v.Color,
                Type = v.Type,
                Capacity = v.Capacity,
                IsActive = v.IsActive,
                DriverId = v.Driver?.Id,
                CreatedAt = v.CreatedAt
            }).ToList();

            return new PagedResult<VehicleDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
    }
}
