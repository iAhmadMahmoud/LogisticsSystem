using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Dispatch.Specifications;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Dispatch.Queries.GetAssignmentHistory
{
    public sealed class GetAssignmentHistoryQueryHandler : IRequestHandler<GetAssignmentHistoryQuery, PagedResult<AssignmentHistoryResponse>>
    {
        private readonly IGenericRepository<DispatchAssignment> _assignmentRepository;

        public GetAssignmentHistoryQueryHandler(IGenericRepository<DispatchAssignment> assignmentRepository)
        {
            _assignmentRepository = assignmentRepository;
        }

        public async Task<PagedResult<AssignmentHistoryResponse>> Handle(GetAssignmentHistoryQuery request, CancellationToken cancellationToken)
        {
            var specification = new AssignmentHistorySpecification(request.ShipmentId,request.PageNumber,request.PageSize);

            var totalCount = await _assignmentRepository.CountAsync(specification,cancellationToken);

            var assignments = await _assignmentRepository.ListAsync(specification, cancellationToken);

            var items = assignments.Select(x => new AssignmentHistoryResponse(
                x.Id,
                x.ShipmentId,
                x.DriverId,
                x.AttemptNumber,
                x.Status.ToString(),
                x.SentAt,
                x.RespondedAt))
                .ToList();

            return new PagedResult<AssignmentHistoryResponse>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };

        }
    }
}
