using MediatR;

namespace LogisticsSystem.Application.Authentication.Commands.ConfirmEmail
{
    public sealed record ConfirmEmailCommand(Guid UserId, string Token) : IRequest<Unit>;
}
