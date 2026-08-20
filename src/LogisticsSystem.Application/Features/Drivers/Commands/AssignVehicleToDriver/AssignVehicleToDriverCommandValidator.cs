using FluentValidation;

namespace LogisticsSystem.Application.Features.Drivers.Commands.AssignVehicleToDriver
{
    public sealed class AssignVehicleToDriverCommandValidator : AbstractValidator<AssignVehicleToDriverCommand>
    {
        public AssignVehicleToDriverCommandValidator()
        {
            RuleFor(x => x.DriverId)
                .NotEmpty().WithMessage("Driver ID is required.");

            RuleFor(x => x.VehicleId)
                .NotEmpty().WithMessage("Vehicle ID is required.");
        }
    }
}
