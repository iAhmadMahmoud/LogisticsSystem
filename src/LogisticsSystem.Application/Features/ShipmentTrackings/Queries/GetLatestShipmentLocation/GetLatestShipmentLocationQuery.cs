using LogisticsSystem.Application.Features.ShipmentTrackings.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.ShipmentTrackings.Queries.GetLatestShipmentLocation
{
    public sealed record GetLatestShipmentLocationQuery(Guid ShipmentId) : IRequest<ShipmentTrackingDto>;
}
