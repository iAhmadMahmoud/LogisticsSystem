using FluentValidation;

namespace LogisticsSystem.Application.Features.Shipments.Commands.UpdateShipment
{
    public class UpdateShipmentCommandValidator : AbstractValidator<UpdateShipmentCommand>
    {
        public UpdateShipmentCommandValidator()
        {
            RuleFor(x => x.Shipment.Id)
                .NotEmpty();

            RuleFor(x => x.Shipment.PickupAddress)
                .NotEmpty()
                .MaximumLength(500);

            RuleFor(x => x.Shipment.PickupAddress)
                .NotEmpty();

            RuleFor(x => x.Shipment.Weight)
                .GreaterThan(0);

            RuleFor(x => x.Shipment.DistanceKm)
                .GreaterThan(0);

            RuleFor(x => x.Shipment.ShippingCost)
                .GreaterThanOrEqualTo(0);
        }       
    }
}
