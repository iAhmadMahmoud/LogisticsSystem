using FluentValidation;

namespace LogisticsSystem.Application.Features.Drivers.Commands.RemoveVehicleFromDriver
{
    public sealed class RemoveVehicleFromDriverCommandValidator : AbstractValidator<RemoveVehicleFromDriverCommand>
    {
        public RemoveVehicleFromDriverCommandValidator()
        {
            RuleFor(x => x.DriverId)
                .NotEmpty().WithMessage("Driver ID is required.");
        }
    }
}
