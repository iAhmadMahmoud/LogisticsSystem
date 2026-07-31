using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
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
        private readonly IGenericRepository<DispatchAssignment> _dispatchAssignmentRepository;
        private readonly IShipmentStatusHistoryService _shipmentStatusHistoryService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public AssignDriverCommandHandler
            (
                IGenericRepository<Shipment> shipmentRepository,
                IGenericRepository<Driver> driverRepository,
                IUnitOfWork unitOfWork
,
                IShipmentStatusHistoryService shipmentStatusHistoryService,
                ICurrentUserService currentUserService,
                IGenericRepository<DispatchAssignment> dispatchAssignmentRepository)
        {
            _shipmentRepository = shipmentRepository;
            _driverRepository = driverRepository;
            _unitOfWork = unitOfWork;
            _shipmentStatusHistoryService = shipmentStatusHistoryService;
            _currentUserService = currentUserService;
            _dispatchAssignmentRepository = dispatchAssignmentRepository;
        }

        public async Task Handle(AssignDriverCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);

            if (shipment is null)
            {
                throw new KeyNotFoundException("Shipment not found.");
            }

            var driver = await _driverRepository.GetByIdAsync(request.DriverId, cancellationToken);

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

            var dispatchAssignment = new DispatchAssignment
            {
                ShipmentId = shipment.Id,
                DriverId = driver.Id,
                AttemptNumber = 1,
                Status = AssignmentStatus.Pending,
                SentAt = DateTime.UtcNow
            };

            _shipmentRepository.Update(shipment);
            _driverRepository.Update(driver);

            await _dispatchAssignmentRepository.AddAsync(dispatchAssignment, cancellationToken);

            await _shipmentStatusHistoryService.AddAsync(shipment, ShipmentStatus.Assigned, _currentUserService.UserId, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
