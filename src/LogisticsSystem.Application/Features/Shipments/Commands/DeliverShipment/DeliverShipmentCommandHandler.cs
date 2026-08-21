using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Shipments.Helpers;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Application.Features.Shipments.Commands.DeliverShipment
{
    public sealed class DeliverShipmentCommandHandler : IRequestHandler<DeliverShipmentCommand>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly IShipmentStatusHistoryService _statusHistoryService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly INotificationService _notificationService;
        private readonly ITrackingRealtimeService _trackingRealtimeService;
        private readonly IUnitOfWork _unitOfWork;

        public DeliverShipmentCommandHandler(
            IGenericRepository<Shipment> shipmentRepository,
            IUnitOfWork unitOfWork,
            IGenericRepository<Driver> driverRepository,
            IShipmentStatusHistoryService statusHistoryService,
            ICurrentUserService currentUserService,
            IGenericRepository<Customer> customerRepository,
            INotificationService notificationService,
            ITrackingRealtimeService trackingRealtimeService)
        {
            _shipmentRepository = shipmentRepository;
            _unitOfWork = unitOfWork;
            _driverRepository = driverRepository;
            _statusHistoryService = statusHistoryService;
            _currentUserService = currentUserService;
            _customerRepository = customerRepository;
            _notificationService = notificationService;
            _trackingRealtimeService = trackingRealtimeService;
        }

        public async Task Handle(DeliverShipmentCommand request, CancellationToken cancellationToken)
        {
            await ShipmentStatusTransitionValidator.StateMutationLock.WaitAsync(cancellationToken);
            try
            {
                var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);

                if (shipment is null)
                    throw new KeyNotFoundException("Shipment not found.");

                if (shipment.DriverId is null)
                    throw new DomainException("Shipment has no assigned driver.");

                var customer = await _customerRepository.GetByIdAsync(shipment.CustomerId, cancellationToken);

                if (customer is null)
                {
                    throw new KeyNotFoundException("Customer not found.");
                }

                var currentDriver = await _driverRepository.FirstOrDefaultAsync(
                    new LogisticsSystem.Application.Features.Drivers.Specifications.DriverByUserIdSpecification(_currentUserService.UserId),
                    cancellationToken);

                if (currentDriver is null)
                {
                    throw new UnauthorizedAccessException("Driver profile not found.");
                }

                if (shipment.DriverId != currentDriver.Id)
                {
                    throw new UnauthorizedAccessException("You are not assigned to this shipment.");
                }

                if (!ShipmentStatusTransitionValidator.CanTransition(shipment.Status, ShipmentStatus.Delivered))
                {
                    throw new DomainException($"Shipment cannot transition from {shipment.Status} to Delivered.");
                }
                shipment.Status = ShipmentStatus.Delivered;
                shipment.DeliveredAt = DateTime.UtcNow;

                currentDriver.Status = DriverStatus.Available;

                _shipmentRepository.Update(shipment);
                _driverRepository.Update(currentDriver);

                await _statusHistoryService.AddAsync(shipment, ShipmentStatus.Delivered, _currentUserService.UserId, cancellationToken);

                await _notificationService.CreateAsync(
                    customer.UserId,
                    "Shipment Delivered",
                    $"Shipment {shipment.TrackingNumber} has been delivered successfully.",
                    NotificationType.ShipmentDelivered,
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _notificationService.SendRealtimeAsync(
                    customer.UserId,
                    "Shipment Delivered",
                    $"Shipment {shipment.TrackingNumber} has been delivered successfully.",
                    cancellationToken);

                await _trackingRealtimeService.ShipmentStatusChangedAsync(
                    shipment.Id,
                    ShipmentStatus.Delivered,
                    DateTime.UtcNow,
                    null,
                    cancellationToken);
            }
            finally
            {
                ShipmentStatusTransitionValidator.StateMutationLock.Release();
            }
        }
    }
}
