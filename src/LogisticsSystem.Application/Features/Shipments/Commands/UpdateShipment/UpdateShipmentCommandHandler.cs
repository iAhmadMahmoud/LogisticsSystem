using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Customers.Specifications;
using LogisticsSystem.Application.Features.Shipments.Specifications;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.UpdateShipment
{
    public class UpdateShipmentCommandHandler : IRequestHandler<UpdateShipmentCommand>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly ICurrentUserService _currentUserService;


        public UpdateShipmentCommandHandler(IGenericRepository<Shipment> shipmentRepository, IUnitOfWork unitOfWork, IGenericRepository<Customer> customerRepository, ICurrentUserService currentUserService)
        {
            _shipmentRepository = shipmentRepository;
            _unitOfWork = unitOfWork;
            _customerRepository = customerRepository;
            _currentUserService = currentUserService;
        }

        public async Task Handle(UpdateShipmentCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Shipment;

            

            var customer = await _customerRepository.FirstOrDefaultAsync(new CustomerByUserIdSpecification(_currentUserService.UserId));

            if(customer is null)
            {
                throw new UnauthorizedAccessException("Customer profile not found.");
            }

            var shipment = await _shipmentRepository.FirstOrDefaultAsync(new ShipmentByIdAndCustomerSpecification(request.Shipment.Id,customer.Id));

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

            _shipmentRepository.Update(shipment);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
