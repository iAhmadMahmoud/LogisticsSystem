using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Dispatch.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.Contracts;

namespace LogisticsSystem.Infrastructure.Services
{
    public sealed class AssignmentExpirationService : IAssignmentExpirationService
    {
        private readonly IGenericRepository<DispatchAssignment> _assignmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly DispatchOptions _options;
        private readonly ILogger<AssignmentExpirationService> _logger;

        public AssignmentExpirationService(IGenericRepository<DispatchAssignment> assignmentRepository, IUnitOfWork unitOfWork, IOptions<DispatchOptions> options, ILogger<AssignmentExpirationService> logger)
        {
            _assignmentRepository = assignmentRepository;
            _unitOfWork = unitOfWork;
            _options = options.Value;
            _logger = logger;
        }

        public async Task ExpireAssignmentsAsync(CancellationToken cancellationToken = default)
        {
            var expirationTime = DateTime.UtcNow.AddMinutes(-_options.AssignmentExpirationMinutes);

            var specification = new ExpiredAssignmentsSpecification(expirationTime);

            var assignments = await _assignmentRepository.ListAsync(specification, cancellationToken);

            if (assignments.Count == 0)
                return;

            var now = DateTime.UtcNow;

            foreach (var assignment in assignments)
            {
                assignment.Status = AssignmentStatus.Expired;
                assignment.RespondedAt = now;

                _assignmentRepository.Update(assignment);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Expired {Count} dispatch assignments.", assignments.Count);
        }
    }
}
