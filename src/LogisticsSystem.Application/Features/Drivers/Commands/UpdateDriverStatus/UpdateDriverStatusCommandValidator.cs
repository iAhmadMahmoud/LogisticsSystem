using FluentValidation;
using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Application.Features.Drivers.Commands.UpdateDriverStatus
{
    public sealed class UpdateDriverStatusCommandValidator :AbstractValidator<UpdateDriverStatusCommand>
    {
        public UpdateDriverStatusCommandValidator()
        {
            RuleFor(x => x.Status)
                .IsInEnum()
                .Must(status =>
                status == DriverStatus.Available ||
                status == DriverStatus.Offline ||
                status == DriverStatus.OnBreak)
                .WithMessage("Drivers can only set their status to Available, Offline, or OnBreak.");
        }
    }
}
