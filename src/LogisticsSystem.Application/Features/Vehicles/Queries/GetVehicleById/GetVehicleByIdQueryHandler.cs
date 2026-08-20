using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Vehicles.DTOs;
using LogisticsSystem.Application.Features.Vehicles.Specifications;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Vehicles.Queries.GetVehicleById
{
    public sealed class GetVehicleByIdQueryHandler : IRequestHandler<GetVehicleByIdQuery, VehicleDto>
    {
        private readonly IGenericRepository<Vehicle> _vehicleRepository;

        public GetVehicleByIdQueryHandler(IGenericRepository<Vehicle> vehicleRepository)
        {
            _vehicleRepository = vehicleRepository;
        }

        public async Task<VehicleDto> Handle(GetVehicleByIdQuery request, CancellationToken cancellationToken)
        {
            var vehicle = await _vehicleRepository.FirstOrDefaultAsync(
                new VehicleByIdWithDriverSpecification(request.Id),
                cancellationToken);

            if (vehicle is null)
            {
                throw new KeyNotFoundException("Vehicle not found.");
            }

            return new VehicleDto
            {
                Id = vehicle.Id,
                PlateNumber = vehicle.PlateNumber,
                Brand = vehicle.Brand,
                Model = vehicle.Model,
                ManufacturingYear = vehicle.ManufacturingYear,
                Color = vehicle.Color,
                Type = vehicle.Type,
                Capacity = vehicle.Capacity,
                IsActive = vehicle.IsActive,
                DriverId = vehicle.Driver?.Id,
                CreatedAt = vehicle.CreatedAt
            };
        }
    }
}
