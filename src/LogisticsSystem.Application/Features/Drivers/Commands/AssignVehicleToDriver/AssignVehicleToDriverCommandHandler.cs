using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Drivers.Specifications;
using LogisticsSystem.Application.Features.Vehicles.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Domain.Exceptions;
using MediatR;

namespace LogisticsSystem.Application.Features.Drivers.Commands.AssignVehicleToDriver
{
    public sealed class AssignVehicleToDriverCommandHandler : IRequestHandler<AssignVehicleToDriverCommand>
    {
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly IGenericRepository<Vehicle> _vehicleRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AssignVehicleToDriverCommandHandler(
            IGenericRepository<Driver> driverRepository,
            IGenericRepository<Vehicle> vehicleRepository,
            IUnitOfWork unitOfWork)
        {
            _driverRepository = driverRepository;
            _vehicleRepository = vehicleRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(AssignVehicleToDriverCommand request, CancellationToken cancellationToken)
        {
            var driver = await _driverRepository.FirstOrDefaultAsync(
                new DriverByIdSpecification(request.DriverId),
                cancellationToken);

            if (driver is null)
            {
                throw new KeyNotFoundException("Driver not found.");
            }

            if (driver.Status == DriverStatus.Suspended)
            {
                throw new DomainException("Cannot assign a vehicle to a suspended driver.");
            }

            if (driver.VehicleId.HasValue && driver.VehicleId.Value != request.VehicleId)
            {
                throw new DomainException("Driver already has an assigned vehicle.");
            }

            var vehicle = await _vehicleRepository.FirstOrDefaultAsync(
                new VehicleByIdWithDriverSpecification(request.VehicleId),
                cancellationToken);

            if (vehicle is null)
            {
                throw new KeyNotFoundException("Vehicle not found.");
            }

            if (!vehicle.IsActive)
            {
                throw new DomainException("Cannot assign an inactive vehicle.");
            }

            if (vehicle.Driver != null && vehicle.Driver.Id != driver.Id)
            {
                throw new DomainException("Vehicle is already assigned to another driver.");
            }

            driver.VehicleId = vehicle.Id;
            _driverRepository.Update(driver);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
