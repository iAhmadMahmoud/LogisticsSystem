using LogisticsSystem.Application.Features.Shipments.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Queries.GetShipmentById
{
    public sealed record GetShipmentByIdQuery(Guid Id) : IRequest<ShipmentDto>;
    
}
