using FluentValidation;
using FluentValidation.Validators;

namespace LogisticsSystem.Application.Authentication.Commands.ForgotPassword
{
    public sealed class ForgotPasswordCommandValidatorv : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordCommandValidatorv()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();
        }
    }
}
