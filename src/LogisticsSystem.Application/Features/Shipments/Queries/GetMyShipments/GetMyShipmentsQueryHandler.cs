using AutoMapper;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Customers.Specifications;
using LogisticsSystem.Application.Features.Shipments.DTOs;
using LogisticsSystem.Application.Features.Shipments.Specifications;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Queries.GetMyShipments
{
    public sealed class GetMyShipmentsQueryHandler : IRequestHandler<GetMyShipmentsQuery, PagedResult<ShipmentDto>>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetMyShipmentsQueryHandler(
            IGenericRepository<Shipment> shipmentRepository,
            IGenericRepository<Customer> customerRepository,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _shipmentRepository = shipmentRepository;
            _customerRepository = customerRepository;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<PagedResult<ShipmentDto>> Handle(GetMyShipmentsQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            var customer = await _customerRepository.FirstOrDefaultAsync(
                new CustomerByUserIdSpecification(userId),
                cancellationToken);

            if (customer is null)
            {
                throw new KeyNotFoundException("Customer profile not found.");
            }

            var specification = new MyShipmentsSpecification(customer.Id, request);

            var totalCount = await _shipmentRepository.CountAsync(specification, cancellationToken);
            var shipments = await _shipmentRepository.ListAsync(specification, cancellationToken);

            var items = _mapper.Map<IReadOnlyList<ShipmentDto>>(shipments);

            return new PagedResult<ShipmentDto>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
