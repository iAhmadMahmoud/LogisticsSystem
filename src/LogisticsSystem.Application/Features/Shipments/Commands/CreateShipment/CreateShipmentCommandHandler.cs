using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.CreateShipment
{
    public class CreateShipmentCommandHandler : IRequestHandler<CreateShipmentCommand, Guid>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateShipmentCommandHandler(IGenericRepository<Shipment> shipmentRepository, IUnitOfWork unitOfWork)
        {
            _shipmentRepository = shipmentRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(CreateShipmentCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Shipment;

            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),

                TrackingNumber = GenerateTrackingNumber(),

                CustomerId = dto.CustomerId,

                PickupAddress = dto.PickupAddress,
                PickupLatitude = dto.PickupLatitude,
                PickupLongitude = dto.PickupLongitude,

                DeliveryAddress = dto.DeliveryAddress,
                DeliveryLatitude = dto.DeliveryLatitude,
                DeliveryLongitude = dto.DeliveryLongitude,

                Weight = dto.Weight,
                DistanceKm = dto.DistanceKm,
                ShippingCost = dto.ShippingCost,

                Priority = dto.Priority,
                Status = ShipmentStatus.Pending,

                Notes = dto.Notes,

                ScheduledAt = dto.ScheduledAt

            };

            await _shipmentRepository.AddAsync(shipment,cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return shipment.Id;
        }
        private static string GenerateTrackingNumber()
        {
            return $"TRK-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        }
    }
}
