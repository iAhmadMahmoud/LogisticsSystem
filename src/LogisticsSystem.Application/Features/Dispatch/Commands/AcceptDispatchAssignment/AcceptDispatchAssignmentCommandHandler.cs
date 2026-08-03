using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Shipments.Helpers;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Application.Features.Dispatch.Commands.AcceptDispatchAssignment
{
    public sealed class AcceptDispatchAssignmentCommandHandler : IRequestHandler<AcceptDispatchAssignmentCommand>
    {
        private readonly IGenericRepository<DispatchAssignment> _dispatchAssignmentRepository;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IShipmentStatusHistoryService _shipmentStatusHistoryService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public AcceptDispatchAssignmentCommandHandler(
            IGenericRepository<DispatchAssignment> dispatchAssignmentRepository,
            IGenericRepository<Driver> driverRepository,
            IGenericRepository<Shipment> shipmentRepository,
            IShipmentStatusHistoryService shipmentStatusHistoryService,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _dispatchAssignmentRepository = dispatchAssignmentRepository;
            _driverRepository = driverRepository;
            _shipmentRepository = shipmentRepository;
            _shipmentStatusHistoryService = shipmentStatusHistoryService;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(AcceptDispatchAssignmentCommand request, CancellationToken cancellationToken)
        {
            // 1. Load the dispatch assignment
            var assignment = await _dispatchAssignmentRepository.GetByIdAsync(request.AssignmentId, cancellationToken);

            if (assignment is null)
            {
                throw new KeyNotFoundException("Dispatch assignment not found.");
            }

            // 2. Only a Pending assignment can be accepted
            if (assignment.Status != AssignmentStatus.Pending)
            {
                throw new InvalidOperationException(
                    $"Dispatch assignment cannot be accepted because its status is '{assignment.Status}'.");
            }

            // 3. Load the current driver profile
            var driver = await _driverRepository
                .AsQueryable()
                .FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId, cancellationToken);

            if (driver is null)
            {
                throw new UnauthorizedAccessException("Driver profile not found.");
            }

            // 4. Ensure the assignment belongs to the current driver
            if (assignment.DriverId != driver.Id)
            {
                throw new UnauthorizedAccessException("You are not authorized to accept this dispatch assignment.");
            }

            // 5. Load the shipment associated with this assignment
            var shipment = await _shipmentRepository.GetByIdAsync(assignment.ShipmentId, cancellationToken);

            if (shipment is null)
            {
                throw new KeyNotFoundException("Shipment not found.");
            }

            // 6. Verify the shipment can still transition to Assigned
            if (!ShipmentStatusTransitionValidator.CanTransition(shipment.Status, ShipmentStatus.Assigned))
            {
                throw new InvalidOperationException(
                    $"Shipment cannot transition from '{shipment.Status}' to 'Assigned'.");
            }

            // 7. Verify the shipment does not already have a driver (another offer was accepted first)
            if (shipment.DriverId is not null)
            {
                throw new InvalidOperationException("Shipment already has an accepted driver assignment.");
            }

            // 8. Verify the driver is still available
            if (driver.Status != DriverStatus.Available)
            {
                throw new InvalidOperationException("Driver is no longer available.");
            }

            // 9. Atomically finalize the assignment
            assignment.Status = AssignmentStatus.Accepted;
            assignment.RespondedAt = DateTime.UtcNow;

            shipment.DriverId = driver.Id;
            shipment.Status = ShipmentStatus.Assigned;
            shipment.AssignedAt = DateTime.UtcNow;

            driver.Status = DriverStatus.Busy;

            _dispatchAssignmentRepository.Update(assignment);
            _shipmentRepository.Update(shipment);
            _driverRepository.Update(driver);

            // 10. Record the shipment status change
            await _shipmentStatusHistoryService.AddAsync(
                shipment,
                ShipmentStatus.Assigned,
                _currentUserService.UserId,
                cancellationToken);

            // 11. Commit all changes in one transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
