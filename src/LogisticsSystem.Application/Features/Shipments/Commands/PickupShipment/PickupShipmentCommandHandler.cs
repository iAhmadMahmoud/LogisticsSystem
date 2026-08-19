using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Shipments.Helpers;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Application.Features.Shipments.Commands.PickupShipment
{
    public sealed class PickupShipmentCommandHandler : IRequestHandler<PickupShipmentCommand>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly IShipmentStatusHistoryService _statusHistoryService;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationService _notificationService;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly ITrackingRealtimeService _trackingRealtimeService;
        private readonly IUnitOfWork _unitOfWork;

        public PickupShipmentCommandHandler(
            IGenericRepository<Shipment> shipmentRepository,
            IUnitOfWork unitOfWork,
            IShipmentStatusHistoryService statusHistoryService,
            ICurrentUserService currentUserService,
            IGenericRepository<Driver> driverRepository,
            INotificationService notificationService,
            IGenericRepository<Customer> customerRepository,
            ITrackingRealtimeService trackingRealtimeService)
        {
            _shipmentRepository = shipmentRepository;
            _unitOfWork = unitOfWork;
            _statusHistoryService = statusHistoryService;
            _currentUserService = currentUserService;
            _driverRepository = driverRepository;
            _notificationService = notificationService;
            _customerRepository = customerRepository;
            _trackingRealtimeService = trackingRealtimeService;
        }

        public async Task Handle(PickupShipmentCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);

            if (shipment is null)
            {
                throw new KeyNotFoundException("Shipment not found.");
            }

            if (shipment.DriverId is null)
            {
                throw new DomainException("Shipment has no assigned driver.");
            }


            var customer = await _customerRepository.GetByIdAsync(shipment.CustomerId, cancellationToken);

            if (customer is null)
            {
                throw new KeyNotFoundException("Customer not found.");
            }

            var driver = await _driverRepository.AsQueryable().FirstOrDefaultAsync(d => d.UserId == _currentUserService.UserId, cancellationToken);

            if (driver is null)
            {
                throw new UnauthorizedAccessException("Driver profile not found.");
            }

            if (shipment.DriverId != driver.Id)
            {
                throw new UnauthorizedAccessException("You are not assigned to this shipment.");
            }

            if (!ShipmentStatusTransitionValidator.CanTransition(shipment.Status, ShipmentStatus.PickedUp))
            {
                throw new DomainException($"Shipment cannot transition from {shipment.Status} to PickedUp.");
            }

            shipment.Status = ShipmentStatus.PickedUp;
            shipment.PickedUpAt = DateTime.UtcNow;

            _shipmentRepository.Update(shipment);

            await _statusHistoryService.AddAsync(shipment, ShipmentStatus.PickedUp, _currentUserService.UserId, cancellationToken);

            await _notificationService.CreateAsync(
                customer.UserId,
                "Shipment Picked Up",
                $"Shipment {shipment.TrackingNumber} has been picked up by the driver.",
                NotificationType.ShipmentPickedUp,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _notificationService.SendRealtimeAsync(customer.UserId, "Shipment Picked Up", $"Shipment {shipment.TrackingNumber} has been picked up by the driver.", cancellationToken);

            await _trackingRealtimeService.ShipmentStatusChangedAsync(
                shipment.Id,
                ShipmentStatus.PickedUp,
                DateTime.UtcNow,
                null,
                cancellationToken);
        }
    }
}
