using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Application.Features.Dispatch.Commands.RejectDispatchAssignment
{
    public sealed class RejectDispatchAssignmentCommandHandler : IRequestHandler<RejectDispatchAssignmentCommand>
    {
        private readonly IGenericRepository<DispatchAssignment> _dispatchAssignmentRepository;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public RejectDispatchAssignmentCommandHandler(
            IGenericRepository<DispatchAssignment> dispatchAssignmentRepository,
            IGenericRepository<Driver> driverRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _dispatchAssignmentRepository = dispatchAssignmentRepository;
            _driverRepository = driverRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(RejectDispatchAssignmentCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _dispatchAssignmentRepository.GetByIdAsync(request.AssignmentId, cancellationToken);
            if (assignment is null)
            {
                throw new KeyNotFoundException("Dispatch assignment not found.");
            }

            if (assignment.Status != AssignmentStatus.Pending)
            {
                throw new InvalidOperationException($"Dispatch assignment cannot be rejected because its status is {assignment.Status}.");
            }

            var driver = await _driverRepository.AsQueryable().FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId, cancellationToken);

            if (driver is null)
            {
                throw new UnauthorizedAccessException("Driver profile not found.");
            }

            if (assignment.DriverId != driver.Id)
            {
                throw new UnauthorizedAccessException("You are not authorized to reject this dispatch assignment.");
            }

            assignment.Status = AssignmentStatus.Rejected;

            assignment.RespondedAt = DateTime.UtcNow;

            _dispatchAssignmentRepository.Update(assignment);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
