using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Application.Features.Dispatch.Commands.RejectDispatchAssignment
{
    public sealed class RejectDispatchAssignmentCommandHandler
        : IRequestHandler<RejectDispatchAssignmentCommand>
    {
        private readonly IGenericRepository<DispatchAssignment> _dispatchAssignmentRepository;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly INotificationService _notificationService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDriverAssignmentService _driverAssignmentService;
        private readonly IDispatchAssignmentService _dispatchAssignmentService;

        public RejectDispatchAssignmentCommandHandler(
            IGenericRepository<DispatchAssignment> dispatchAssignmentRepository,
            IGenericRepository<Driver> driverRepository,
            IGenericRepository<Shipment> shipmentRepository,
            IGenericRepository<Customer> customerRepository,
            INotificationService notificationService,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            IDriverAssignmentService driverAssignmentService,
            IDispatchAssignmentService dispatchAssignmentService)
        {
            _dispatchAssignmentRepository = dispatchAssignmentRepository;
            _driverRepository = driverRepository;
            _shipmentRepository = shipmentRepository;
            _customerRepository = customerRepository;
            _notificationService = notificationService;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _driverAssignmentService = driverAssignmentService;
            _dispatchAssignmentService = dispatchAssignmentService;
        }

        public async Task Handle(
            RejectDispatchAssignmentCommand request,
            CancellationToken cancellationToken)
        {
            await LogisticsSystem.Application.Features.Shipments.Helpers.ShipmentStatusTransitionValidator.StateMutationLock.WaitAsync(cancellationToken);
            try
            {
                // 1. Load assignment
                var assignment = await _dispatchAssignmentRepository
                    .GetByIdAsync(request.AssignmentId, cancellationToken);

                if (assignment is null)
                {
                    throw new KeyNotFoundException(
                        "Dispatch assignment not found.");
                }

                // 2. Make sure assignment is still pending
                if (assignment.Status != AssignmentStatus.Pending)
                {
                    throw new DomainException(
                        $"Dispatch assignment cannot be rejected because its status is {assignment.Status}.");
                }

                // 3. Get current driver's profile
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

                // 4. Make sure this assignment belongs to the current driver
                if (assignment.DriverId != driver.Id)
                {
                    throw new UnauthorizedAccessException(
                        "You are not authorized to reject this dispatch assignment.");
                }

                // 5. Load shipment explicitly
                var shipment = await _shipmentRepository
                    .GetByIdAsync(assignment.ShipmentId, cancellationToken);

                if (shipment is null)
                {
                    throw new KeyNotFoundException(
                        "Shipment not found.");
                }

                // 6. Reject current assignment
                assignment.Status = AssignmentStatus.Rejected;
                assignment.RespondedAt = DateTime.UtcNow;

                _dispatchAssignmentRepository.Update(assignment);

                // 7. Find the next available driver
                var nextDriver =
                    await _driverAssignmentService.FindBestAvailableDriverAsync(
                        shipment,
                        cancellationToken);

                // 8. Create a new assignment if another driver is available, otherwise notify customer
                if (nextDriver is not null)
                {
                    await _dispatchAssignmentService.CreateAssignmentAsync(
                        shipment,
                        nextDriver,
                        cancellationToken);
                }
                else
                {
                    var customer = await _customerRepository.GetByIdAsync(
                        shipment.CustomerId,
                        cancellationToken);

                    if (customer is not null)
                    {
                        await _notificationService.CreateAsync(
                            customer.UserId,
                            "No Driver Available",
                            $"Unable to find an available driver for shipment {shipment.TrackingNumber}.",
                            NotificationType.NoDriverAvailable,
                            cancellationToken);

                        await _notificationService.SendRealtimeAsync(
                            customer.UserId,
                            "No Driver Available",
                            $"Unable to find an available driver for shipment {shipment.TrackingNumber}.",
                            cancellationToken);
                    }
                }

                // 9. Save rejection + new assignment or notification
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                LogisticsSystem.Application.Features.Shipments.Helpers.ShipmentStatusTransitionValidator.StateMutationLock.Release();
            }
        }
    }
}