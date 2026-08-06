using FluentValidation;

namespace LogisticsSystem.Application.Features.Drivers.Commands.UpdateDriverLocation
{
    public sealed class UpdateDriverLocationCommandValidator : AbstractValidator<UpdateDriverLocationCommand>
    {
        public UpdateDriverLocationCommandValidator()
        {
            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180);
        }
    }
}
