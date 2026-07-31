using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Shipments.Helpers;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.StartTransit
{
    public sealed class StartTransitCommandHandler : IRequestHandler<StartTransitCommand>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IShipmentStatusHistoryService _statusHistoryService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public StartTransitCommandHandler(IGenericRepository<Shipment> shipmentRepository, IUnitOfWork unitOfWork, IShipmentStatusHistoryService statusHistoryService, ICurrentUserService currentUserService)
        {
            _shipmentRepository = shipmentRepository;
            _unitOfWork = unitOfWork;
            _statusHistoryService = statusHistoryService;
            _currentUserService = currentUserService;
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
                throw new InvalidOperationException("Shipment has no assigned driver.");
            }

            if (!ShipmentStatusTransitionValidator.CanTransition(shipment.Status, ShipmentStatus.InTransit))
            {
                throw new InvalidOperationException($"Shipment cannot transition from {shipment.Status} to InTransit.");
            }

            shipment.Status = ShipmentStatus.InTransit;

            _shipmentRepository.Update(shipment);

            await _statusHistoryService.AddAsync(shipment,ShipmentStatus.InTransit, _currentUserService.UserId, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
