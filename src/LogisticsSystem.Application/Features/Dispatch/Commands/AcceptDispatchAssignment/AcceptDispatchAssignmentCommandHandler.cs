using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Application.Features.Dispatch.Commands.AcceptDispatchAssignment
{
    public sealed class AcceptDispatchAssignmentCommandHandler : IRequestHandler<AcceptDispatchAssignmentCommand>
    {
        private readonly IGenericRepository<DispatchAssignment> _dispatchAssignmentRepository;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public AcceptDispatchAssignmentCommandHandler(
            IGenericRepository<DispatchAssignment> dispatchAssignmentRepository,
            ICurrentUserService currentUserService,
            IGenericRepository<Driver> driverRepository,
            IUnitOfWork unitOfWork)
        {
            _dispatchAssignmentRepository = dispatchAssignmentRepository;
            _currentUserService = currentUserService;
            _driverRepository = driverRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(AcceptDispatchAssignmentCommand request, CancellationToken cancellationToken)
        {
            // 1. Get the dispatch assignment
            var assignment = await _dispatchAssignmentRepository.GetByIdAsync(request.AssignmentId,cancellationToken);
            if(assignment is null)
            {
                throw new KeyNotFoundException("Dispatch assignment not found.");
            }

            // 2. Only pending assignments can be accepted
            if(assignment.Status != AssignmentStatus.Pending)
            {
                throw new InvalidOperationException($"Dispatch assignment cannot be accepted because its status is {assignment.Status}");
            }

            // 3. Get the current driver's profile
            var driver = await _driverRepository.AsQueryable().FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId, cancellationToken);

            if(driver is null)
            {
                throw new UnauthorizedAccessException("Driver profile not found.");
            }

            // 4. Ensure the assignment belongs to the current driver
            if (assignment.DriverId != driver.Id)
            {
                throw new UnauthorizedAccessException("You are not authorized to accept this dispatch assignment.");
            }

            // 5. Accept the assignment
            assignment.Status = AssignmentStatus.Accepted;

            assignment.RespondedAt = DateTime.UtcNow;

            _dispatchAssignmentRepository.Update(assignment);

            // 6. Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
