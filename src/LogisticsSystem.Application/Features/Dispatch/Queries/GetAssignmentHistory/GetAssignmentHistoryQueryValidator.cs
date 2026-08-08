using FluentValidation;

namespace LogisticsSystem.Application.Features.Dispatch.Queries.GetAssignmentHistory
{
    public sealed class GetAssignmentHistoryQueryValidator : AbstractValidator<GetAssignmentHistoryQuery>
    {
        public GetAssignmentHistoryQueryValidator()
        {
            RuleFor(x => x.ShipmentId)
                .NotEmpty();

            RuleFor(x => x.PageNumber)
                .GreaterThan(0);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);
        }
    }
}
