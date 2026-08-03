using MediatR;

namespace LogisticsSystem.Application.Features.Dispatch.Commands.RejectDispatchAssignment
{
    public sealed record RejectDispatchAssignmentCommand(Guid AssignmentId) : IRequest;
}
