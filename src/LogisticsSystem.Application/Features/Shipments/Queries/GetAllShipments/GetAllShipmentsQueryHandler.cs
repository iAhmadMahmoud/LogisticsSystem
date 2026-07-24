using AutoMapper;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Shipments.DTOs;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Queries.GetAllShipments
{
    public sealed class GetAllShipmentsQueryHandler : IRequestHandler<GetAllShipmentsQuery, IReadOnlyList<ShipmentDto>>
    {
        private readonly IGenericRepository<Shipment> _repository;
        private readonly IMapper _mapper;

        public GetAllShipmentsQueryHandler(IGenericRepository<Shipment> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<ShipmentDto>> Handle(GetAllShipmentsQuery request, CancellationToken cancellationToken)
        {
            var shipments = await _repository.GetAllAsync(cancellationToken);

            var map = _mapper.Map<IReadOnlyList<ShipmentDto>>(shipments);

            return map;
        }
    }
}
