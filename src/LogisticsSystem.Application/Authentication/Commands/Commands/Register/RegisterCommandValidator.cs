using FluentValidation;

namespace LogisticsSystem.Application.Authentication.Commands.Commands.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Request.FirstName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Request.LastName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Request.Username)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(50);

            RuleFor(x => x.Request.Email)
                .NotEmpty()
                .MinimumLength(8);
        }
    }
}
