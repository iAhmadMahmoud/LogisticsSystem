using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Dispatch.Specifications;
using LogisticsSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Application.Features.Dispatch.Queries.GetMyAssignments
{
    public sealed class GetMyAssignmentsQueryHandler : IRequestHandler<GetMyAssignmentsQuery, PagedResult<DispatchAssignmentResponse>>
    {
        private readonly IGenericRepository<DispatchAssignment> _assignmentRepository;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly ICurrentUserService _currentUserService;

        public GetMyAssignmentsQueryHandler(IGenericRepository<Driver> driverRepository, ICurrentUserService currentUserService, IGenericRepository<DispatchAssignment> assignmentRepository)
        {
            
            _driverRepository = driverRepository;
            _currentUserService = currentUserService;
            _assignmentRepository = assignmentRepository;
        }

        public async Task<PagedResult<DispatchAssignmentResponse>> Handle(GetMyAssignmentsQuery request, CancellationToken cancellationToken)
        {
            var driver = await _driverRepository.AsQueryable().FirstOrDefaultAsync(x=>x.UserId == _currentUserService.UserId,cancellationToken);

            if(driver is null)
            {
                throw new UnauthorizedAccessException("Driver profile not found.");
            }

            var specification = new MyAssignmentsSpecification(driver.Id,request.Status,request.PageNumber,request.PageSize);

            var totalCount = await _assignmentRepository.CountAsync(specification,cancellationToken);

            var assignments = await _assignmentRepository.ListAsync(specification,cancellationToken);

            var items = assignments.Select(x=> new DispatchAssignmentResponse
            {
                Id=x.Id,
                ShipmentId=x.ShipmentId,
                DriverId=x.DriverId,
                AttemptNumber=x.AttemptNumber,
                Status=x.Status,
                SentAt=x.SentAt,
                RespondedAt=x.RespondedAt
            }).ToList();

            return new PagedResult<DispatchAssignmentResponse>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
