using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Shipments.Helpers;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.AssignDriver
{
    public sealed class AssignDriverCommandHandler : IRequestHandler<AssignDriverCommand>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AssignDriverCommandHandler
            (
                IGenericRepository<Shipment> shipmentRepository,
                IGenericRepository<Driver> driverRepository,
                IUnitOfWork unitOfWork
            )
        {
            _shipmentRepository = shipmentRepository;
            _driverRepository = driverRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(AssignDriverCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId);

            if (shipment is null)
            {
                throw new KeyNotFoundException("Shipment not found.");
            }

            var driver = await _driverRepository.GetByIdAsync(request.DriverId);

            if (driver is null)
            {
                throw new KeyNotFoundException("Driver not found.");
            }

            if (shipment.DriverId is not null)
            {
                throw new InvalidOperationException(
                    "Shipment already has a driver assigned.");
            }

            if (!ShipmentStatusTransitionValidator.CanTransition(shipment.Status, ShipmentStatus.Assigned))
            {
                throw new InvalidOperationException($"Shipment cannot transition from {shipment.Status} to Assigned.");
            }

            if (driver.Status != DriverStatus.Available)
            {
                throw new InvalidOperationException("Driver is not available.");
            }

            shipment.DriverId = driver.Id;
            shipment.Status = ShipmentStatus.Assigned;
            shipment.AssignedAt = DateTime.UtcNow;

            driver.Status = DriverStatus.Busy;

            _shipmentRepository.Update(shipment);
            _driverRepository.Update(driver);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
