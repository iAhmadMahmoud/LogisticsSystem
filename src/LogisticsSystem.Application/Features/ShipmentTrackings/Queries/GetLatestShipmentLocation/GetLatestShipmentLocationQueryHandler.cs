using AutoMapper;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Customers.Specifications;
using LogisticsSystem.Application.Features.Shipments.Specifications;
using LogisticsSystem.Application.Features.ShipmentTrackings.DTOs;
using LogisticsSystem.Application.Features.ShipmentTrackings.Specifications;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.ShipmentTrackings.Queries.GetLatestShipmentLocation
{
    public sealed class GetLatestShipmentLocationQueryHandler : IRequestHandler<GetLatestShipmentLocationQuery, ShipmentTrackingDto>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IGenericRepository<ShipmentTracking> _trackingRepository;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetLatestShipmentLocationQueryHandler(IGenericRepository<Shipment> shipmentRepository, IGenericRepository<ShipmentTracking> trackingRepository, IGenericRepository<Customer> customerRepository, ICurrentUserService currentUserService, IMapper mapper)
        {
            _shipmentRepository = shipmentRepository;
            _trackingRepository = trackingRepository;
            _customerRepository = customerRepository;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<ShipmentTrackingDto> Handle(GetLatestShipmentLocationQuery request, CancellationToken cancellationToken)
        {
            var customer = await _customerRepository.FirstOrDefaultAsync(new CustomerByUserIdSpecification(_currentUserService.UserId),cancellationToken);

            if(customer is null)
            {
                throw new UnauthorizedAccessException("Customer profile not found.");
            }

            var shipment = await _shipmentRepository.FirstOrDefaultAsync(new ShipmentByIdAndCustomerSpecification(request.ShipmentId, customer.Id),cancellationToken);

            if (shipment is null)
            {
                throw new KeyNotFoundException("Shipment not found.");
            }

            var latestTracking  = await _trackingRepository.FirstOrDefaultAsync(new LatestShipmentTrackingSpecification(shipment.Id),cancellationToken);

            if(latestTracking is null)
            {
                throw new KeyNotFoundException("No tracking location found for this shipment.");
            }

            return _mapper.Map<ShipmentTrackingDto>(latestTracking);
        }
    }
}
