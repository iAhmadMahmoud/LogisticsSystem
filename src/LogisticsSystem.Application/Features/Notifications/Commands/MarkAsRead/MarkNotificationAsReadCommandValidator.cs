using FluentValidation;

namespace LogisticsSystem.Application.Features.Notifications.Commands.MarkAsRead
{
    public sealed class MarkNotificationAsReadCommandValidator :AbstractValidator<MarkNotificationAsReadCommand>
    {
        public MarkNotificationAsReadCommandValidator()
        {
            RuleFor(x=>x.notificationId).NotEmpty();
        }
    }
}
