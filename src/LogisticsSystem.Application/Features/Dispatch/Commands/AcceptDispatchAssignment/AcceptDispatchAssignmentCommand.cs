using MediatR;

namespace LogisticsSystem.Application.Features.Dispatch.Commands.AcceptDispatchAssignment
{
    public sealed record AcceptDispatchAssignmentCommand(Guid AssignmentId) : IRequest;
}
