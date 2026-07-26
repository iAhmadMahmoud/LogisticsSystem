using LogisticsSystem.Application.Common.Models.Authentication;
using MediatR;

namespace LogisticsSystem.Application.Authentication.Commands.Login
{
    public sealed record LoginCommand(LoginRequest Request) : IRequest<AuthenticationResult>;

}
