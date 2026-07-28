using AutoMapper;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Customers.Specifications;
using LogisticsSystem.Application.Features.Shipments.DTOs;
using LogisticsSystem.Application.Features.Shipments.Specifications;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Queries.GetShipmentById
{
    public sealed class GetShipmentByIdHandler : IRequestHandler<GetShipmentByIdQuery, ShipmentDto>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetShipmentByIdHandler
            (
                IGenericRepository<Shipment> shipmentRepository,
                IMapper mapper,
                IGenericRepository<Customer> customerRepository,
                ICurrentUserService currentUserService
            )
        {
            _shipmentRepository = shipmentRepository;
            _mapper = mapper;
            _customerRepository = customerRepository;
            _currentUserService = currentUserService;
        }

        public async Task<ShipmentDto> Handle(GetShipmentByIdQuery request, CancellationToken cancellationToken)
        {
           
            var customer = await _customerRepository.FirstOrDefaultAsync(new CustomerByUserIdSpecification(_currentUserService.UserId));

            if (customer is null)
            {
                throw new UnauthorizedAccessException("Customer profile not found.");
            }

            var shipment = await _shipmentRepository.FirstOrDefaultAsync(
                new ShipmentByIdAndCustomerSpecification(
                    request.Id,
                    customer.Id));

            if (shipment is null)
            {
                throw new KeyNotFoundException("Shipment not found.");
            }


            var map = _mapper.Map<ShipmentDto>(shipment);

            return map;
        }
    }
}
