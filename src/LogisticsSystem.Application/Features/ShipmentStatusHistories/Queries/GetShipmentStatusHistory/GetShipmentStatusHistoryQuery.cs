using LogisticsSystem.Application.Features.ShipmentStatusHistories.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.ShipmentStatusHistories.Queries.GetShipmentStatusHistory
{
    public sealed record GetShipmentStatusHistoryQuery(Guid ShipmentId) : IRequest<IReadOnlyList<ShipmentStatusHistoryDto>>;
}
