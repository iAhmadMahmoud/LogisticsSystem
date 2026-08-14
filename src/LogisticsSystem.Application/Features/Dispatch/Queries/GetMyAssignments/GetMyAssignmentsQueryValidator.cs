using FluentValidation;

namespace LogisticsSystem.Application.Features.Dispatch.Queries.GetMyAssignments
{
    public sealed class GetMyAssignmentsQueryValidator : AbstractValidator<GetMyAssignmentsQuery>
    {
        public GetMyAssignmentsQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 50);
        }
    }
}
