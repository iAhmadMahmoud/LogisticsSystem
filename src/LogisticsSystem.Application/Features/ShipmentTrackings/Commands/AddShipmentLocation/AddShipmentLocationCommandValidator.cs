using FluentValidation;

namespace LogisticsSystem.Application.Features.ShipmentTrackings.Commands.AddShipmentLocation
{
    public sealed class AddShipmentLocationCommandValidator : AbstractValidator<AddShipmentLocationCommand>
    {
        public AddShipmentLocationCommandValidator()
        {
            RuleFor(x => x.ShipmentId)
                .NotEmpty()
                .WithMessage("Shipment ID is required.");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90)
                .WithMessage("Latitude must be between -90 and 90.");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                .WithMessage("Longitude must be between -180 and 180.");
        }
    }
}
