using FluentValidation;

namespace LogisticsSystem.Application.Features.Shipments.Commands.DeliverShipment
{
    public sealed class DeliverShipmentCommandValidator : AbstractValidator<DeliverShipmentCommand>
    {
        public DeliverShipmentCommandValidator()
        {
            RuleFor(x=>x.ShipmentId).NotEmpty();
        }
    }
}
