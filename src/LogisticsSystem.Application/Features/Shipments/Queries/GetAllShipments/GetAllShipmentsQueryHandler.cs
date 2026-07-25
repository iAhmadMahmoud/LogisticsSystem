using AutoMapper;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Shipments.DTOs;
using LogisticsSystem.Application.Features.Shipments.Specifications;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Queries.GetAllShipments
{
    public sealed class GetAllShipmentsQueryHandler : IRequestHandler<GetAllShipmentsQuery, PagedResult<ShipmentDto>>
    {
        private readonly IGenericRepository<Shipment> _repository;
        private readonly IMapper _mapper;

        public GetAllShipmentsQueryHandler(IGenericRepository<Shipment> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PagedResult<ShipmentDto>> Handle(GetAllShipmentsQuery request, CancellationToken cancellationToken)
        {
            var specification = new ShipmentSpecification(request);

            var totalCount = await _repository.CountAsync(specification, cancellationToken);
            var shipments = await _repository.ListAsync(specification, cancellationToken);

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
