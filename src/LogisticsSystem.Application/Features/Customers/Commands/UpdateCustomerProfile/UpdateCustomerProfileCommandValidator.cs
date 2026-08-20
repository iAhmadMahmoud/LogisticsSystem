using FluentValidation;

namespace LogisticsSystem.Application.Features.Customers.Commands.UpdateCustomerProfile
{
    public sealed class UpdateCustomerProfileCommandValidator : AbstractValidator<UpdateCustomerProfileCommand>
    {
        public UpdateCustomerProfileCommandValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.");

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.DefaultAddress)
                .MaximumLength(200).WithMessage("Default address must not exceed 200 characters.")
                .When(x => !string.IsNullOrEmpty(x.DefaultAddress));
        }
    }
}
