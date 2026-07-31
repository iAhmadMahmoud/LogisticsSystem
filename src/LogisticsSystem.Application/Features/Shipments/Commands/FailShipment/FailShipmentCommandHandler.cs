using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Shipments.Helpers;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.FailShipment
{
    public sealed class FailShipmentCommandHandler : IRequestHandler<FailShipmentCommand>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly IShipmentStatusHistoryService _statusHistoryService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public FailShipmentCommandHandler(IGenericRepository<Shipment> shipmentRepository, IGenericRepository<Driver> driverRepository, IUnitOfWork unitOfWork, IShipmentStatusHistoryService statusHistoryService, ICurrentUserService currentUserService)
        {
            _shipmentRepository = shipmentRepository;
            _driverRepository = driverRepository;
            _unitOfWork = unitOfWork;
            _statusHistoryService = statusHistoryService;
            _currentUserService = currentUserService;
        }

        public async Task Handle(FailShipmentCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId);

            if (shipment is null)
            {
                throw new KeyNotFoundException("Shipment not found.");
            }

            if(shipment.DriverId is null)
            {
                throw new InvalidOperationException("Shipment has no assigned driver.");
            }

            if (!ShipmentStatusTransitionValidator.CanTransition(shipment.Status, ShipmentStatus.Failed))
            {
                throw new InvalidOperationException($"Shipment cannot transition from {shipment.Status} to Failed.");
            }

            var driver = await _driverRepository.GetByIdAsync(shipment.DriverId.Value);

            if(driver is null)
            {
                throw new KeyNotFoundException("Driver not found.");
            }

            shipment.Status = ShipmentStatus.Failed;
            driver.Status = DriverStatus.Available;
            
            _shipmentRepository.Update(shipment);
            _driverRepository.Update(driver);

            await _statusHistoryService.AddAsync(shipment, ShipmentStatus.Failed, _currentUserService.UserId, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
