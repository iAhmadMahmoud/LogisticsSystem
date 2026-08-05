using FluentValidation;

namespace LogisticsSystem.Application.Features.Drivers.Queries.GetAllDrivers
{
    public sealed class GetAllDriversQueryValidator : AbstractValidator<GetAllDriversQuery>
    {
        public GetAllDriversQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);
        }
    }
}
