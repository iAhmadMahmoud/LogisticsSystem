
using FluentValidation;

namespace LogisticsSystem.Application.Features.Notifications.Queries.GetMyNotifications
{
    public sealed class GetMyNotificationsQueryValidator :AbstractValidator<GetMyNotificationsQuery>
    {
        public GetMyNotificationsQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);
        }
    }
}
