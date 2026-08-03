using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Shipments.Helpers;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
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
        private readonly IUnitOfWork _unitOfWork;
        public DeliverShipmentCommandHandler(IGenericRepository<Shipment> shipmentRepository, IUnitOfWork unitOfWork, IGenericRepository<Driver> driverRepository, IShipmentStatusHistoryService statusHistoryService, ICurrentUserService currentUserService)
        {
            _shipmentRepository = shipmentRepository;
            _unitOfWork = unitOfWork;
            _driverRepository = driverRepository;
            _statusHistoryService = statusHistoryService;
            _currentUserService = currentUserService;
        }

        public async Task Handle(DeliverShipmentCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId);

            if (shipment is null)
                throw new KeyNotFoundException("Shipment not found.");

            if (shipment.DriverId is null)
                throw new InvalidOperationException("Shipment has no assigned driver.");

        

            var currentDriver = await _driverRepository.AsQueryable().FirstOrDefaultAsync(d => d.UserId == _currentUserService.UserId, cancellationToken);

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
                throw new InvalidOperationException($"Shipment cannot transition from {shipment.Status} to Delivered.");
            }
            shipment.Status = ShipmentStatus.Delivered;
            shipment.DeliveredAt = DateTime.UtcNow;

            currentDriver.Status = DriverStatus.Available;

            _shipmentRepository.Update(shipment);
            _driverRepository.Update(currentDriver);

            await _statusHistoryService.AddAsync(shipment, ShipmentStatus.Delivered, _currentUserService.UserId, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
