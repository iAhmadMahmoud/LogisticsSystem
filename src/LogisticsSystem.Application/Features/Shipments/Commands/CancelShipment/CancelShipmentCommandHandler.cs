using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Customers.Specifications;
using LogisticsSystem.Application.Features.Shipments.Helpers;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Domain.Exceptions;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.CancelShipment
{
    public sealed class CancelShipmentCommandHandler : IRequestHandler<CancelShipmentCommand>
    {

        private readonly IGenericRepository<Shipment> _shipmentsRepository;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly IShipmentStatusHistoryService _statusHistoryService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly INotificationService _notificationService;
        private readonly ITrackingRealtimeService _trackingRealtimeService;
        private readonly IUnitOfWork _unitOfWork;

        public CancelShipmentCommandHandler(
            IGenericRepository<Shipment> shipmentsRepository,
            ICurrentUserService currentUserService,
            IShipmentStatusHistoryService statusHistoryService,
            IGenericRepository<Driver> driverRepository,
            IUnitOfWork unitOfWork,
            IGenericRepository<Customer> customerRepository,
            INotificationService notificationService,
            ITrackingRealtimeService trackingRealtimeService)
        {
            _shipmentsRepository = shipmentsRepository;
            _currentUserService = currentUserService;
            _statusHistoryService = statusHistoryService;
            _driverRepository = driverRepository;
            _unitOfWork = unitOfWork;
            _customerRepository = customerRepository;
            _notificationService = notificationService;
            _trackingRealtimeService = trackingRealtimeService;
        }

        public async Task Handle(CancelShipmentCommand request, CancellationToken cancellationToken)
        {
            await ShipmentStatusTransitionValidator.StateMutationLock.WaitAsync(cancellationToken);
            try
            {
                var shipment = await _shipmentsRepository.GetByIdAsync(request.ShipmentId, cancellationToken);

                if (shipment is null)
                {
                    throw new KeyNotFoundException("Shipment not found.");
                }
                Customer? customer;
                if (_currentUserService.IsInRole(Roles.Customer))
                {
                     customer = await _customerRepository.FirstOrDefaultAsync(
                        new CustomerByUserIdSpecification(
                            _currentUserService.UserId),
                        cancellationToken);

                    if (customer is null)
                    {
                        throw new UnauthorizedAccessException(
                            "Customer profile not found.");
                    }

                    if (shipment.CustomerId != customer.Id)
                    {
                        throw new UnauthorizedAccessException("You are not allowed to cancel this shipment.");
                    }
                }

                customer = await _customerRepository.GetByIdAsync(shipment.CustomerId, cancellationToken);

                if (customer is null)
                {
                    throw new KeyNotFoundException("Customer not found.");
                }

                if (!ShipmentStatusTransitionValidator.CanTransition(shipment.Status, ShipmentStatus.Cancelled))
                {
                    throw new DomainException($"Shipment cannot transition from {shipment.Status} to Cancelled.");
                }

                var wasAssigned = shipment.Status == ShipmentStatus.Assigned;

                shipment.Status = ShipmentStatus.Cancelled;
                shipment.CancelledAt = DateTime.UtcNow;

                _shipmentsRepository.Update(shipment);
                Driver? driver = null;
                if (wasAssigned)
                {
                    if (shipment.DriverId is null)
                    {
                        throw new DomainException("Assigned shipment has no driver.");
                    }

                    driver = await _driverRepository.GetByIdAsync(shipment.DriverId.Value, cancellationToken);

                    if (driver is null)
                    {
                        throw new KeyNotFoundException("Assigned driver not found.");
                    }

                    driver.Status = DriverStatus.Available;
                    _driverRepository.Update(driver);
                }

                await _statusHistoryService.AddAsync(shipment, ShipmentStatus.Cancelled, _currentUserService.UserId, cancellationToken);

                await _notificationService.CreateAsync(
                    customer.UserId,
                    "Shipment Cancelled",
                    $"Shipment {shipment.TrackingNumber} has been cancelled.",
                    NotificationType.ShipmentCancelled,
                    cancellationToken);

                if (driver is not null)
                {
                    await _notificationService.CreateAsync(
                        driver.UserId,
                        "Shipment Cancelled",
                        $"Shipment {shipment.TrackingNumber} has been cancelled by the customer.",
                        NotificationType.ShipmentCancelled,
                        cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _notificationService.SendRealtimeAsync(
                    customer.UserId,
                    "Shipment Cancelled",
                    $"Shipment {shipment.TrackingNumber} has been cancelled.",
                    cancellationToken);

                if (driver is not null)
                {
                    await _notificationService.SendRealtimeAsync(
                        driver.UserId,
                        "Shipment Cancelled",
                        $"Shipment {shipment.TrackingNumber} has been cancelled by the customer.",
                        cancellationToken);
                }

                await _trackingRealtimeService.ShipmentStatusChangedAsync(
                    shipment.Id,
                    ShipmentStatus.Cancelled,
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
