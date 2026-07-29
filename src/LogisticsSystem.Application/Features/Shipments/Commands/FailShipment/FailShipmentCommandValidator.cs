using FluentValidation;

namespace LogisticsSystem.Application.Features.Shipments.Commands.FailShipment
{
    public sealed class FailShipmentCommandValidator : AbstractValidator<FailShipmentCommand>
    {
        public FailShipmentCommandValidator()
        {
            RuleFor(x=>x.ShipmentId).NotEmpty();
        }
    }
}
