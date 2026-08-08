using LogisticsSystem.Application.Common.Models;
using MediatR;

namespace LogisticsSystem.Application.Features.Dispatch.Queries.GetAssignmentHistory
{
    public sealed record GetAssignmentHistoryQuery(Guid ShipmentId, int PageNumber = 1, int PageSize = 10) : IRequest<PagedResult<AssignmentHistoryResponse>>;
    
}
