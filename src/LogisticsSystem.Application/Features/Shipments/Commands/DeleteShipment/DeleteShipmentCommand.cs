using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.DeleteShipment
{
    public record DeleteShipmentCommand(Guid Id) : IRequest;
}
