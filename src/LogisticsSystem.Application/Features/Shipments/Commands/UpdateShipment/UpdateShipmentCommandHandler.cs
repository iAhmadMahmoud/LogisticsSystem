using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Customers.Specifications;
using LogisticsSystem.Application.Features.Shipments.Specifications;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.UpdateShipment
{
    public class UpdateShipmentCommandHandler : IRequestHandler<UpdateShipmentCommand>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateShipmentCommandHandler(
            IGenericRepository<Shipment> shipmentRepository,
            IUnitOfWork unitOfWork,
            IGenericRepository<Customer> customerRepository,
            ICurrentUserService currentUserService)
        {
            _shipmentRepository = shipmentRepository;
            _unitOfWork = unitOfWork;
            _customerRepository = customerRepository;
            _currentUserService = currentUserService;
        }

        public async Task Handle(UpdateShipmentCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Shipment;

            Shipment? shipment;

            // Dispatchers and Admins can update any shipment regardless of ownership.
            // Customers are restricted to their own shipments only.
            if (_currentUserService.IsInRole(Roles.Dispatcher) || _currentUserService.IsInRole(Roles.Admin))
            {
                shipment = await _shipmentRepository.GetByIdAsync(dto.Id);

                if (shipment is null)
                    throw new KeyNotFoundException("Shipment not found.");
            }
            else
            {
                // Customer path — must own the shipment
                var customer = await _customerRepository.FirstOrDefaultAsync(
                    new CustomerByUserIdSpecification(_currentUserService.UserId),
                    cancellationToken);

                if (customer is null)
                    throw new UnauthorizedAccessException("Customer profile not found.");

                shipment = await _shipmentRepository.FirstOrDefaultAsync(
                    new ShipmentByIdAndCustomerSpecification(dto.Id, customer.Id),
                    cancellationToken);

                if (shipment is null)
                    throw new KeyNotFoundException("Shipment not found.");
            }

            if (shipment.Status != ShipmentStatus.Pending)
                throw new InvalidOperationException("Only pending shipments can be updated.");

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
