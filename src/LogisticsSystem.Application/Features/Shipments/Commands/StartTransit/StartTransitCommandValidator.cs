using FluentValidation;

namespace LogisticsSystem.Application.Features.Shipments.Commands.StartTransit
{
    public sealed class StartTransitCommandValidator : AbstractValidator<StartTransitCommand>
    {
        public StartTransitCommandValidator()
        {
            RuleFor(x=>x.ShipmentId).NotEmpty();
        }
    }
}
