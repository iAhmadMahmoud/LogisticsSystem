using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Vehicles.DTOs;
using LogisticsSystem.Application.Features.Vehicles.Specifications;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Vehicles.Commands.CreateVehicle
{
    public sealed class CreateVehicleCommandHandler : IRequestHandler<CreateVehicleCommand, VehicleDto>
    {
        private readonly IGenericRepository<Vehicle> _vehicleRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateVehicleCommandHandler(
            IGenericRepository<Vehicle> vehicleRepository,
            IUnitOfWork unitOfWork)
        {
            _vehicleRepository = vehicleRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<VehicleDto> Handle(CreateVehicleCommand request, CancellationToken cancellationToken)
        {
            var normalizedPlateNumber = request.PlateNumber.Trim().ToUpperInvariant();

            var existingVehicle = await _vehicleRepository.FirstOrDefaultAsync(
                new VehicleByPlateNumberSpecification(normalizedPlateNumber),
                cancellationToken);

            if (existingVehicle != null)
            {
                throw new InvalidOperationException($"Vehicle with plate number '{request.PlateNumber}' already exists.");
            }

            var vehicle = new Vehicle
            {
                PlateNumber = normalizedPlateNumber,
                Brand = request.Brand.Trim(),
                Model = request.Model.Trim(),
                ManufacturingYear = request.ManufacturingYear,
                Color = request.Color.Trim(),
                Type = request.Type,
                Capacity = request.Capacity,
                IsActive = true
            };

            await _vehicleRepository.AddAsync(vehicle, cancellationToken);
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
                DriverId = null,
                CreatedAt = vehicle.CreatedAt
            };
        }
    }
}
