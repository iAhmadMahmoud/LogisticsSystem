using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Dispatch.Queries.GetMyAssignments
{
    public sealed record GetMyAssignmentsQuery(int PageNumber = 1, int PageSize = 10, AssignmentStatus? Status = null) : IRequest<PagedResult<DispatchAssignmentResponse>>;
}
