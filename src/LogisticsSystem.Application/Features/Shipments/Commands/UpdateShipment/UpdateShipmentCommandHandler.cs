using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.UpdateShipment
{
    public class UpdateShipmentCommandHandler : IRequestHandler<UpdateShipmentCommand>
    {
        private readonly IGenericRepository<Shipment> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateShipmentCommandHandler(IGenericRepository<Shipment> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(UpdateShipmentCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Shipment;

            var shipment = await _repository.GetByIdAsync(
                dto.Id,
                cancellationToken);

            if (shipment is null)
                throw new KeyNotFoundException("Shipment not found.");

            // Update editable properties
            shipment.PickupAddress = dto.PickupAddress;
            shipment.PickupLatitude = dto.PickupLatitude;
            shipment.PickupLongitude = dto.PickupLongitude;

            shipment.DeliveryAddress = dto.DeliveryAddress;
            shipment.DeliveryLatitude = dto.DeliveryLatitude;
            shipment.DeliveryLongitude = dto.DeliveryLongitude;

            shipment.Weight = dto.Weight;
            shipment.DistanceKm = dto.DistanceKm;
            shipment.ShippingCost = dto.ShippingCost;

            shipment.Priority = dto.Priority;
            shipment.ScheduledAt = dto.ScheduledAt;
            shipment.Notes = dto.Notes;

            _repository.Update(shipment);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
