using MediatR;

namespace LogisticsSystem.Application.Authentication.Commands.ResetPassword
{
    public sealed record ResetPasswordCommand(Guid UserId, string Token, string NewPassword) : IRequest<Unit>;
}
