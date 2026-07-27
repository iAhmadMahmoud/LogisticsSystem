using MediatR;

namespace LogisticsSystem.Application.Authentication.Commands.Logout
{
    public sealed record LogoutCommand(string RefreshToken) : IRequest<Unit>;
}
