using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Shipments.Helpers;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Application.Features.Shipments.Commands.AssignDriver
{
    public sealed class AssignDriverCommandHandler : IRequestHandler<AssignDriverCommand>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly IGenericRepository<DispatchAssignment> _dispatchAssignmentRepository;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;

        public AssignDriverCommandHandler(
            IGenericRepository<Shipment> shipmentRepository,
            IGenericRepository<Driver> driverRepository,
            IGenericRepository<DispatchAssignment> dispatchAssignmentRepository,
            IUnitOfWork unitOfWork,
            INotificationService notificationService)
        {
            _shipmentRepository = shipmentRepository;
            _driverRepository = driverRepository;
            _dispatchAssignmentRepository = dispatchAssignmentRepository;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        public async Task Handle(AssignDriverCommand request, CancellationToken cancellationToken)
        {
            // 1. Load the shipment
            var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);

            if (shipment is null)
            {
                throw new KeyNotFoundException("Shipment not found.");
            }

            // 2. Load the driver
            var driver = await _driverRepository.GetByIdAsync(request.DriverId, cancellationToken);

            if (driver is null)
            {
                throw new KeyNotFoundException("Driver not found.");
            }

            // 3. Verify the shipment is in a state that can receive a dispatch offer
            if (!ShipmentStatusTransitionValidator.CanTransition(shipment.Status, ShipmentStatus.Assigned))
            {
                throw new DomainException(
                    $"A dispatch offer cannot be sent for a shipment with status '{shipment.Status}'.");
            }

            // 4. Verify the shipment does not already have an accepted driver
            if (shipment.DriverId is not null)
            {
                throw new DomainException("Shipment already has an accepted driver assignment.");
            }

            // 5. Verify the driver is available
            if (driver.Status != DriverStatus.Available)
            {
                throw new DomainException("Driver is not available.");
            }

            // 6. Prevent duplicate active (Pending) offers for the same shipment
            var hasPendingAssignment = await _dispatchAssignmentRepository
                .AsQueryable()
                .AnyAsync(x => x.ShipmentId == request.ShipmentId && x.Status == AssignmentStatus.Pending, cancellationToken);

            if (hasPendingAssignment)
            {
                throw new DomainException(
                    "A pending dispatch offer already exists for this shipment. Wait for the driver to respond before sending another offer.");
            }

            // 7. Calculate the next AttemptNumber for this shipment
            var attemptNumber = await _dispatchAssignmentRepository
                .AsQueryable()
                .CountAsync(x => x.ShipmentId == request.ShipmentId, cancellationToken) + 1;

            // 8. Create a Pending dispatch offer — shipment and driver state do NOT change here
            var dispatchAssignment = new DispatchAssignment
            {
                ShipmentId = shipment.Id,
                DriverId = driver.Id,
                AttemptNumber = attemptNumber,
                Status = AssignmentStatus.Pending,
                SentAt = DateTime.UtcNow
            };

            await _dispatchAssignmentRepository.AddAsync(dispatchAssignment, cancellationToken);

            await _notificationService.CreateAsync(
                driver.UserId,
                "New Shipment Assignment",
                $"You have a new shipment assignment: {shipment.TrackingNumber}.",
                NotificationType.DispatchAssignmentReceived,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _notificationService.SendRealtimeAsync(
                driver.UserId,
                "New Shipment Assignment",
                $"You have a new shipment assignment: {shipment.TrackingNumber}.",
                cancellationToken);
        }
    }
}
