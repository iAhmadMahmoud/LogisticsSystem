using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Shipments.Helpers;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.CancelShipment
{
    public sealed class CancelShipmentCommandHandler : IRequestHandler<CancelShipmentCommand>
    {

        private readonly IGenericRepository<Shipment> _shipmentsRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CancelShipmentCommandHandler(IGenericRepository<Shipment> shipmentsRepository, IUnitOfWork unitOfWork)
        {
            _shipmentsRepository = shipmentsRepository;
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

            shipment.Status = ShipmentStatus.Cancelled;
            shipment.CancelledAt = DateTime.UtcNow;

            _shipmentsRepository.Update(shipment);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
