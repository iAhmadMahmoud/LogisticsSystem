using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Shipments.Helpers;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Application.Features.Shipments.Commands.StartTransit
{
    public sealed class StartTransitCommandHandler : IRequestHandler<StartTransitCommand>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly IShipmentStatusHistoryService _statusHistoryService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public StartTransitCommandHandler(IGenericRepository<Shipment> shipmentRepository, IUnitOfWork unitOfWork, IShipmentStatusHistoryService statusHistoryService, ICurrentUserService currentUserService, IGenericRepository<Driver> driverRepository)
        {
            _shipmentRepository = shipmentRepository;
            _unitOfWork = unitOfWork;
            _statusHistoryService = statusHistoryService;
            _currentUserService = currentUserService;
            _driverRepository = driverRepository;
        }

        public async Task Handle(StartTransitCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId);

            if (shipment is null)
            {
                throw new KeyNotFoundException("Shipment not found.");
            }

            if(shipment.DriverId is null)
            {
                throw new DomainException("Shipment has no assigned driver.");
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


            if (!ShipmentStatusTransitionValidator.CanTransition(shipment.Status, ShipmentStatus.InTransit))
            {
                throw new DomainException($"Shipment cannot transition from {shipment.Status} to InTransit.");
            }

            shipment.Status = ShipmentStatus.InTransit;

            _shipmentRepository.Update(shipment);

            await _statusHistoryService.AddAsync(shipment,ShipmentStatus.InTransit, _currentUserService.UserId, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
