using MediatR;

namespace LogisticsSystem.Application.Authentication.Commands.ForgotPassword
{
    public sealed record ForgotPasswordCommand(string Email) : IRequest<Unit>;
    
}
