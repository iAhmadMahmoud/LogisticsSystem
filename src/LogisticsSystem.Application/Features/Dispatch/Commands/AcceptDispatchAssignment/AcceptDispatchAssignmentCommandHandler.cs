using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Shipments.Helpers;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Application.Features.Dispatch.Commands.AcceptDispatchAssignment
{
    public sealed class AcceptDispatchAssignmentCommandHandler
        : IRequestHandler<AcceptDispatchAssignmentCommand>
    {
        private readonly IGenericRepository<DispatchAssignment> _dispatchAssignmentRepository;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly IShipmentStatusHistoryService _shipmentStatusHistoryService;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;

        public AcceptDispatchAssignmentCommandHandler(
            IGenericRepository<DispatchAssignment> dispatchAssignmentRepository,
            IGenericRepository<Driver> driverRepository,
            IGenericRepository<Shipment> shipmentRepository,
            IGenericRepository<Customer> customerRepository,
            IShipmentStatusHistoryService shipmentStatusHistoryService,
            ICurrentUserService currentUserService,
            INotificationService notificationService,
            IUnitOfWork unitOfWork)
        {
            _dispatchAssignmentRepository = dispatchAssignmentRepository;
            _driverRepository = driverRepository;
            _shipmentRepository = shipmentRepository;
            _customerRepository = customerRepository;
            _shipmentStatusHistoryService = shipmentStatusHistoryService;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            AcceptDispatchAssignmentCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Load the dispatch assignment
            var assignment = await _dispatchAssignmentRepository
                .GetByIdAsync(
                    request.AssignmentId,
                    cancellationToken);

            if (assignment is null)
            {
                throw new KeyNotFoundException(
                    "Dispatch assignment not found.");
            }

            // 2. Only pending assignments can be accepted
            if (assignment.Status != AssignmentStatus.Pending)
            {
                throw new DomainException(
                    $"Dispatch assignment cannot be accepted because its status is '{assignment.Status}'.");
            }

            // 3. Load the current driver
            var driver = await _driverRepository
                .AsQueryable()
                .FirstOrDefaultAsync(
                    x => x.UserId == _currentUserService.UserId,
                    cancellationToken);

            if (driver is null)
            {
                throw new UnauthorizedAccessException(
                    "Driver profile not found.");
            }

            // 4. Verify that the assignment belongs to this driver
            if (assignment.DriverId != driver.Id)
            {
                throw new UnauthorizedAccessException(
                    "You are not authorized to accept this dispatch assignment.");
            }

            // 5. Load the shipment
            var shipment = await _shipmentRepository
                .GetByIdAsync(
                    assignment.ShipmentId,
                    cancellationToken);

            if (shipment is null)
            {
                throw new KeyNotFoundException(
                    "Shipment not found.");
            }

            // 6. Verify shipment status transition
            if (!ShipmentStatusTransitionValidator.CanTransition(
                    shipment.Status,
                    ShipmentStatus.Assigned))
            {
                throw new DomainException(
                    $"Shipment cannot transition from '{shipment.Status}' to 'Assigned'.");
            }

            // 7. Make sure another driver hasn't already accepted it
            if (shipment.DriverId is not null)
            {
                throw new DomainException(
                    "Shipment already has an accepted driver assignment.");
            }

            // 8. Verify driver availability
            if (driver.Status != DriverStatus.Available)
            {
                throw new DomainException(
                    "Driver is no longer available.");
            }

            // 9. Load the customer
            var customer = await _customerRepository
                .GetByIdAsync(
                    shipment.CustomerId,
                    cancellationToken);

            if (customer is null)
            {
                throw new KeyNotFoundException(
                    "Customer not found.");
            }

            // 10. Accept the assignment
            assignment.Status = AssignmentStatus.Accepted;
            assignment.RespondedAt = DateTime.UtcNow;

            // 11. Assign the driver to the shipment
            shipment.DriverId = driver.Id;
            shipment.Status = ShipmentStatus.Assigned;
            shipment.AssignedAt = DateTime.UtcNow;

            // 12. Mark driver as busy
            driver.Status = DriverStatus.Busy;

            _dispatchAssignmentRepository.Update(assignment);
            _shipmentRepository.Update(shipment);
            _driverRepository.Update(driver);

            // 13. Record shipment status history
            await _shipmentStatusHistoryService.AddAsync(
                shipment,
                ShipmentStatus.Assigned,
                _currentUserService.UserId,
                cancellationToken);

            // 14. Create notification for the customer
            await _notificationService.CreateAsync(
                customer.UserId,
                "Shipment Assigned",
                $"Shipment {shipment.TrackingNumber} has been assigned to a driver.",
                NotificationType.ShipmentAssigned,
                cancellationToken);

            // 15. Save everything first
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            // 16. Send realtime notification to customer
            await _notificationService.SendRealtimeAsync(
                customer.UserId,
                "Shipment Assigned",
                $"Shipment {shipment.TrackingNumber} has been assigned to a driver.",
                cancellationToken);
        }
    }
}