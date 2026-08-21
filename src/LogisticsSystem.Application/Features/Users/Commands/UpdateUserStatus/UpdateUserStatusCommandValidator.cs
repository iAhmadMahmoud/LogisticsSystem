using FluentValidation;

namespace LogisticsSystem.Application.Features.Users.Commands.UpdateUserStatus
{
    public sealed class UpdateUserStatusCommandValidator : AbstractValidator<UpdateUserStatusCommand>
    {
        public UpdateUserStatusCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("User ID is required.");
        }
    }
}
