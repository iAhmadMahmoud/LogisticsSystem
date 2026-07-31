using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Shipments.Helpers;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.CancelShipment
{
    public sealed class CancelShipmentCommandHandler : IRequestHandler<CancelShipmentCommand>
    {

        private readonly IGenericRepository<Shipment> _shipmentsRepository;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly IShipmentStatusHistoryService _statusHistoryService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public CancelShipmentCommandHandler(
            IGenericRepository<Shipment> shipmentsRepository,
            ICurrentUserService currentUserService,
            IShipmentStatusHistoryService statusHistoryService,
            IGenericRepository<Driver> driverRepository,
            IUnitOfWork unitOfWork)
        {
            _shipmentsRepository = shipmentsRepository;
            _currentUserService = currentUserService;
            _statusHistoryService = statusHistoryService;
            _driverRepository = driverRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(CancelShipmentCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _shipmentsRepository.GetByIdAsync(request.ShipmentId);

            if (shipment is null)
            {
                throw new KeyNotFoundException("Shipment not found.");
            }

            if (!ShipmentStatusTransitionValidator.CanTransition(shipment.Status, ShipmentStatus.Cancelled))
            {
                throw new InvalidOperationException($"Shipment cannot transtion from {shipment.Status} to Cancelled.");
            }

            var wasAssigned = shipment.Status == ShipmentStatus.Assigned;

            shipment.Status = ShipmentStatus.Cancelled;
            shipment.CancelledAt = DateTime.UtcNow;

            _shipmentsRepository.Update(shipment);

            if (wasAssigned)
            {
                if (shipment.DriverId is null)
                {
                    throw new InvalidOperationException("Assigned shipment has no driver.");
                }

                var driver = await _driverRepository.GetByIdAsync(shipment.DriverId.Value, cancellationToken);

                if (driver is null)
                {
                    throw new KeyNotFoundException("Assigned driver not found.");
                }

                driver.Status = DriverStatus.Available;
                _driverRepository.Update(driver);
            }

            await _statusHistoryService.AddAsync(shipment, ShipmentStatus.Cancelled, _currentUserService.UserId, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
