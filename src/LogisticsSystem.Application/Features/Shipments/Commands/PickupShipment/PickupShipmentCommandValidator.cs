using FluentValidation;

namespace LogisticsSystem.Application.Features.Shipments.Commands.PickupShipment
{
    public sealed class PickupShipmentCommandValidator : AbstractValidator<PickupShipmentCommand>
    {
        public PickupShipmentCommandValidator()
        {
            RuleFor(x => x.ShipmentId)
                .NotEmpty();
        }
    }
}
