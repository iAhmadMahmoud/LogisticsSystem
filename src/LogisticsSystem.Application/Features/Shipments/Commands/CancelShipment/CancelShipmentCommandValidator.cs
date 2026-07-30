using FluentValidation;

namespace LogisticsSystem.Application.Features.Shipments.Commands.CancelShipment
{
    public sealed class CancelShipmentCommandValidator : AbstractValidator<CancelShipmentCommand>
    {
        public CancelShipmentCommandValidator()
        {
            RuleFor(x=>x.ShipmentId).NotEmpty();
        }
    }
}
