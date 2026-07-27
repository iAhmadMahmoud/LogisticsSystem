using MediatR;

namespace LogisticsSystem.Application.Authentication.Commands.ChangePassword
{
    public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;
    
}
