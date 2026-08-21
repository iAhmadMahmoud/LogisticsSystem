using FluentValidation;

namespace LogisticsSystem.Application.Features.Vehicles.Commands.CreateVehicle
{
    public sealed class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
    {
        public CreateVehicleCommandValidator()
        {
            RuleFor(x => x.PlateNumber)
                .NotEmpty().WithMessage("Plate number is required.")
                .MaximumLength(50).WithMessage("Plate number cannot exceed 50 characters.");

            RuleFor(x => x.Brand)
                .NotEmpty().WithMessage("Brand is required.")
                .MaximumLength(50).WithMessage("Brand cannot exceed 50 characters.");

            RuleFor(x => x.Model)
                .NotEmpty().WithMessage("Model is required.")
                .MaximumLength(50).WithMessage("Model cannot exceed 50 characters.");

            RuleFor(x => x.ManufacturingYear)
                .GreaterThan(1900).WithMessage("Manufacturing year must be after 1900.")
                .LessThanOrEqualTo(DateTime.UtcNow.Year + 1).WithMessage($"Manufacturing year cannot exceed {DateTime.UtcNow.Year + 1}.");

            RuleFor(x => x.Color)
                .NotEmpty().WithMessage("Color is required.")
                .MaximumLength(30).WithMessage("Color cannot exceed 30 characters.");

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid vehicle type.");

            RuleFor(x => x.Capacity)
                .GreaterThan(0).WithMessage("Capacity must be greater than 0.");
        }
    }
}
