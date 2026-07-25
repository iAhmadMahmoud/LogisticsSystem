using AutoMapper;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Shipments.DTOs;
using LogisticsSystem.Domain.Entities;
using MediatR;
using Microsoft.IdentityModel.Tokens;

namespace LogisticsSystem.Application.Features.Shipments.Queries.GetShipmentById
{
    public sealed class GetShipmentByIdHandler : IRequestHandler<GetShipmentByIdQuery, ShipmentDto>
    {
        private readonly IGenericRepository<Shipment> _repository;
        private readonly IMapper _mapper;

        public GetShipmentByIdHandler(IGenericRepository<Shipment> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ShipmentDto> Handle(GetShipmentByIdQuery request, CancellationToken cancellationToken)
        {
            var shipment = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (shipment is null)
            {
                throw new KeyNotFoundException("Shipment not found.");
            }

            var map = _mapper.Map<ShipmentDto>(shipment);

            return map;
        }
    }
}
