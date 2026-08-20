using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Vehicles.DTOs;
using LogisticsSystem.Application.Features.Vehicles.Specifications;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Vehicles.Commands.UpdateVehicle
{
    public sealed class UpdateVehicleCommandHandler : IRequestHandler<UpdateVehicleCommand, VehicleDto>
    {
        private readonly IGenericRepository<Vehicle> _vehicleRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateVehicleCommandHandler(
            IGenericRepository<Vehicle> vehicleRepository,
            IUnitOfWork unitOfWork)
        {
            _vehicleRepository = vehicleRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<VehicleDto> Handle(UpdateVehicleCommand request, CancellationToken cancellationToken)
        {
            var vehicle = await _vehicleRepository.FirstOrDefaultAsync(
                new VehicleByIdWithDriverSpecification(request.Id),
                cancellationToken);

            if (vehicle is null)
            {
                throw new KeyNotFoundException("Vehicle not found.");
            }

            var normalizedPlateNumber = request.PlateNumber.Trim().ToUpperInvariant();

            if (!string.Equals(vehicle.PlateNumber, normalizedPlateNumber, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _vehicleRepository.FirstOrDefaultAsync(
                    new VehicleByPlateNumberSpecification(normalizedPlateNumber),
                    cancellationToken);

                if (existing != null && existing.Id != vehicle.Id)
                {
                    throw new InvalidOperationException($"Vehicle with plate number '{request.PlateNumber}' already exists.");
                }
            }

            vehicle.PlateNumber = normalizedPlateNumber;
            vehicle.Brand = request.Brand.Trim();
            vehicle.Model = request.Model.Trim();
            vehicle.ManufacturingYear = request.ManufacturingYear;
            vehicle.Color = request.Color.Trim();
            vehicle.Type = request.Type;
            vehicle.Capacity = request.Capacity;
            vehicle.IsActive = request.IsActive;

            _vehicleRepository.Update(vehicle);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

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
