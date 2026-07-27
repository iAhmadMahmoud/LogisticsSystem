using LogisticsSystem.Application.Common.Models.Authentication;
using MediatR;

namespace LogisticsSystem.Application.Authentication.Commands.RefreshToken
{
    public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthenticationResult>;
}
