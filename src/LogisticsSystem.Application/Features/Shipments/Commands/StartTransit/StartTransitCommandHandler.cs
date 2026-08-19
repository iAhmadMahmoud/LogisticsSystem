using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Drivers.Specifications;
using LogisticsSystem.Application.Features.Shipments.Helpers;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Domain.Exceptions;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.StartTransit
{
    public sealed class StartTransitCommandHandler : IRequestHandler<StartTransitCommand>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly IShipmentStatusHistoryService _statusHistoryService;
        private readonly INotificationService _notificationService;
        private readonly ITrackingRealtimeService _trackingRealtimeService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public StartTransitCommandHandler(
            IGenericRepository<Shipment> shipmentRepository,
            IGenericRepository<Driver> driverRepository,
            IGenericRepository<Customer> customerRepository,
            IShipmentStatusHistoryService statusHistoryService,
            INotificationService notificationService,
            ITrackingRealtimeService trackingRealtimeService,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _shipmentRepository = shipmentRepository;
            _driverRepository = driverRepository;
            _customerRepository = customerRepository;
            _statusHistoryService = statusHistoryService;
            _notificationService = notificationService;
            _trackingRealtimeService = trackingRealtimeService;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(StartTransitCommand request, CancellationToken cancellationToken)
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

            var driver = await _driverRepository.FirstOrDefaultAsync(
                new DriverByUserIdSpecification(_currentUserService.UserId),
                cancellationToken);

            if (driver is null)
            {
                throw new UnauthorizedAccessException("Driver profile not found.");
            }

            if (shipment.DriverId != driver.Id)
            {
                throw new UnauthorizedAccessException("You are not assigned to this shipment.");
            }

            if (!ShipmentStatusTransitionValidator.CanTransition(shipment.Status, ShipmentStatus.InTransit))
            {
                throw new DomainException($"Shipment cannot transition from {shipment.Status} to InTransit.");
            }

            shipment.Status = ShipmentStatus.InTransit;

            _shipmentRepository.Update(shipment);

            await _statusHistoryService.AddAsync(shipment, ShipmentStatus.InTransit, _currentUserService.UserId, cancellationToken);

            await _notificationService.CreateAsync(
                customer.UserId,
                "Shipment In Transit",
                $"Shipment {shipment.TrackingNumber} is now in transit.",
                NotificationType.ShipmentInTransit,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _notificationService.SendRealtimeAsync(
                customer.UserId,
                "Shipment In Transit",
                $"Shipment {shipment.TrackingNumber} is now in transit.",
                cancellationToken);

            await _trackingRealtimeService.ShipmentStatusChangedAsync(
                shipment.Id,
                ShipmentStatus.InTransit,
                DateTime.UtcNow,
                null,
                cancellationToken);
        }
    }
}

