using LogisticsSystem.Application.Features.Shipments.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.UpdateShipment
{
    public sealed record UpdateShipmentCommand(UpdateShipmentDto Shipment) : IRequest;
}
