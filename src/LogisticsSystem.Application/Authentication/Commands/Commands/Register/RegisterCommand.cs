using LogisticsSystem.Application.Common.Models.Authentication;
using MediatR;

namespace LogisticsSystem.Application.Authentication.Commands.Commands.Register
{
    public sealed record RegisterCommand(RegisterRequest Request) : IRequest<AuthenticationResult>;
    
}
