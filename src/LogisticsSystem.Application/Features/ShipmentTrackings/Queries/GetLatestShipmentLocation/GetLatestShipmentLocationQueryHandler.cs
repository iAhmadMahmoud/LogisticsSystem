using AutoMapper;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Customers.Specifications;
using LogisticsSystem.Application.Features.Shipments.Specifications;
using LogisticsSystem.Application.Features.ShipmentTrackings.DTOs;
using LogisticsSystem.Application.Features.ShipmentTrackings.Specifications;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Application.Features.ShipmentTrackings.Queries.GetLatestShipmentLocation
{
    public sealed class GetLatestShipmentLocationQueryHandler : IRequestHandler<GetLatestShipmentLocationQuery, ShipmentTrackingDto>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IGenericRepository<ShipmentTracking> _trackingRepository;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetLatestShipmentLocationQueryHandler(IGenericRepository<Shipment> shipmentRepository, IGenericRepository<ShipmentTracking> trackingRepository, IGenericRepository<Customer> customerRepository, ICurrentUserService currentUserService, IMapper mapper, IGenericRepository<Driver> driverRepository)
        {
            _shipmentRepository = shipmentRepository;
            _trackingRepository = trackingRepository;
            _customerRepository = customerRepository;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _driverRepository = driverRepository;
        }

        public async Task<ShipmentTrackingDto> Handle(GetLatestShipmentLocationQuery request, CancellationToken cancellationToken)
        {
            Shipment? shipment;

            var canViewAllShipments =
                _currentUserService.IsInRole(Roles.Dispatcher) ||
                _currentUserService.IsInRole(Roles.Admin);

            if (canViewAllShipments)
            {
                shipment = await _shipmentRepository.GetByIdAsync(
                    request.ShipmentId,
                    cancellationToken);
            }
            else if (_currentUserService.IsInRole(Roles.Driver))
            {
                var driver = await _driverRepository
                    .AsQueryable()
                    .FirstOrDefaultAsync(
                        d => d.UserId == _currentUserService.UserId,
                        cancellationToken);

                if (driver is null)
                {
                    throw new UnauthorizedAccessException(
                        "Driver profile not found.");
                }

                shipment = await _shipmentRepository
                    .AsQueryable()
                    .FirstOrDefaultAsync(
                        s => s.Id == request.ShipmentId &&
                             s.DriverId == driver.Id,
                        cancellationToken);
            }
            else
            {
                var customer = await _customerRepository
                    .FirstOrDefaultAsync(
                        new CustomerByUserIdSpecification(
                            _currentUserService.UserId),
                        cancellationToken);

                if (customer is null)
                {
                    throw new UnauthorizedAccessException(
                        "Customer profile not found.");
                }

                shipment = await _shipmentRepository
                    .FirstOrDefaultAsync(
                        new ShipmentByIdAndCustomerSpecification(
                            request.ShipmentId,
                            customer.Id),
                        cancellationToken);
            }

            if (shipment is null)
            {
                throw new KeyNotFoundException(
                    "Shipment not found.");
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
