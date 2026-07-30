using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Customers.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.CreateShipment
{
    public class CreateShipmentCommandHandler : IRequestHandler<CreateShipmentCommand, Guid>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IGenericRepository<Customer> _customerRepository;

        public CreateShipmentCommandHandler
            (
                IGenericRepository<Shipment> shipmentRepository,
                IUnitOfWork unitOfWork,
                ICurrentUserService currentUserService,
                IGenericRepository<Customer> customerRepository
            )
        {
            _shipmentRepository = shipmentRepository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _customerRepository = customerRepository;
        }

        public async Task<Guid> Handle(CreateShipmentCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Shipment;

            var specification = new CustomerByUserIdSpecification(_currentUserService.UserId);

            var customer = await _customerRepository.FirstOrDefaultAsync(specification);

            if (customer is null)
            {
                throw new UnauthorizedAccessException("Customer profile not found.");
            }

            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),

                TrackingNumber = GenerateTrackingNumber(),

                CustomerId = customer.Id,

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
