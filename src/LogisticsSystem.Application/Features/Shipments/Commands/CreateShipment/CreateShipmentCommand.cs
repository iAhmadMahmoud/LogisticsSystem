using LogisticsSystem.Application.Features.Shipments.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.CreateShipment
{
    public record CreateShipmentCommand(CreateShipmentDto Shipment) : IRequest<Guid>;
    
}
