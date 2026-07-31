using MediatR;

namespace LogisticsSystem.Application.Features.ShipmentTrackings.Commands.AddShipmentLocation
{
    public sealed record AddShipmentLocationCommand(Guid ShipmentId, double Latitude, double Longitude) : IRequest;
}
