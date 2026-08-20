using MediatR;

namespace LogisticsSystem.Application.Features.Users.Commands.DeleteUser
{
    public sealed record DeleteUserCommand(Guid Id) : IRequest;
}
