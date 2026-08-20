using AutoMapper;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Customers.Specifications;
using LogisticsSystem.Application.Features.Shipments.DTOs;
using LogisticsSystem.Application.Features.Shipments.Specifications;
using LogisticsSystem.Application.Features.ShipmentTrackings.DTOs;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Queries.GetShipmentById
{
    public sealed class GetShipmentByIdHandler : IRequestHandler<GetShipmentByIdQuery, ShipmentDto>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IIdentityService _identityService;
        private readonly IMapper _mapper;

        public GetShipmentByIdHandler(
            IGenericRepository<Shipment> shipmentRepository,
            IMapper mapper,
            IGenericRepository<Customer> customerRepository,
            ICurrentUserService currentUserService,
            IIdentityService identityService)
        {
            _shipmentRepository = shipmentRepository;
            _mapper = mapper;
            _customerRepository = customerRepository;
            _currentUserService = currentUserService;
            _identityService = identityService;
        }

        public async Task<ShipmentDto> Handle(GetShipmentByIdQuery request, CancellationToken cancellationToken)
        {
            Shipment? shipment;

            if (_currentUserService.IsInRole(Roles.Customer))
            {
                var customer = await _customerRepository.FirstOrDefaultAsync(
                    new CustomerByUserIdSpecification(_currentUserService.UserId),
                    cancellationToken);

                if (customer is null)
                {
                    throw new UnauthorizedAccessException("Customer profile not found.");
                }

                shipment = await _shipmentRepository.FirstOrDefaultAsync(
                    new ShipmentByIdAndCustomerSpecification(request.Id, customer.Id),
                    cancellationToken);
            }
            else
            {
                shipment = await _shipmentRepository.FirstOrDefaultAsync(
                    new ShipmentByIdWithDetailsSpecification(request.Id),
                    cancellationToken);
            }

            if (shipment is null)
            {
                throw new KeyNotFoundException("Shipment not found.");
            }

            var dto = _mapper.Map<ShipmentDto>(shipment);

            if (shipment.Driver is not null)
            {
                dto.DriverLicense = shipment.Driver.LicenseNumber;

                var driverUser = await _identityService.GetUserByIdAsync(shipment.Driver.UserId, cancellationToken);
                if (driverUser is not null)
                {
                    dto.DriverName = driverUser.FullName;
                    dto.DriverPhone = driverUser.PhoneNumber;
                }
            }

            var latestTracking = shipment.ShipmentTrackings?
                .OrderByDescending(t => t.RecordedAt)
                .FirstOrDefault();

            if (latestTracking is not null)
            {
                dto.LatestLocation = new ShipmentTrackingDto
                {
                    Id = latestTracking.Id,
                    ShipmentId = latestTracking.ShipmentId,
                    Latitude = latestTracking.Latitude,
                    Longitude = latestTracking.Longitude,
                    RecordedAt = latestTracking.RecordedAt
                };
            }
            else if (shipment.Driver?.Latitude is not null && shipment.Driver.Longitude is not null)
            {
                dto.LatestLocation = new ShipmentTrackingDto
                {
                    ShipmentId = shipment.Id,
                    Latitude = shipment.Driver.Latitude.Value,
                    Longitude = shipment.Driver.Longitude.Value,
                    RecordedAt = DateTime.UtcNow
                };
            }

            return dto;
        }
    }
}
