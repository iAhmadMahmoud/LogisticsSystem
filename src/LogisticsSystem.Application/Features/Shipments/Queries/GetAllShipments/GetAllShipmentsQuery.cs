using LogisticsSystem.Application.Features.Shipments.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Queries.GetAllShipments
{
    public sealed record GetAllShipmentsQuery : IRequest<IReadOnlyList<ShipmentDto>>;
}
