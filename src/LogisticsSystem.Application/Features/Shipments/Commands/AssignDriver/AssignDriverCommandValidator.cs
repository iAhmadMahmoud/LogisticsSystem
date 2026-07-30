using FluentValidation;

namespace LogisticsSystem.Application.Features.Shipments.Commands.AssignDriver
{
    public sealed class AssignDriverCommandValidator : AbstractValidator<AssignDriverCommand>
    {
        public AssignDriverCommandValidator()
        {
            RuleFor(x => x.ShipmentId)
                .NotEmpty();

            RuleFor(x => x.DriverId)
                .NotEmpty();
        }
    }
}
