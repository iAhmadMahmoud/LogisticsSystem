using FluentValidation;

namespace LogisticsSystem.Application.Features.Dispatch.Commands.AcceptDispatchAssignment
{
    public sealed class AcceptDispatchAssignmentCommandValidator : AbstractValidator<AcceptDispatchAssignmentCommand>
    {
        public AcceptDispatchAssignmentCommandValidator()
        {
            RuleFor(x=>x.AssignmentId).NotEmpty();
        }
    }
}
