using System.Diagnostics;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Dispatch.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogisticsSystem.Infrastructure.Services
{
    public sealed class AssignmentExpirationService : IAssignmentExpirationService
    {
        private readonly IGenericRepository<DispatchAssignment> _assignmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly DispatchOptions _options;
        private readonly ILogger<AssignmentExpirationService> _logger;
        private readonly IDispatchAssignmentService _dispatchAssignmentService;
        private readonly INotificationService _notificationService;
        private readonly IDriverAssignmentService _driverAssignmentService;

        public AssignmentExpirationService(
            IGenericRepository<DispatchAssignment> assignmentRepository,
            IUnitOfWork unitOfWork,
            IOptions<DispatchOptions> options,
            ILogger<AssignmentExpirationService> logger,
            IDispatchAssignmentService dispatchAssignmentService,
            INotificationService notificationService,
            IDriverAssignmentService driverAssignmentService)
        {
            _assignmentRepository = assignmentRepository;
            _unitOfWork = unitOfWork;
            _options = options.Value;
            _logger = logger;
            _dispatchAssignmentService = dispatchAssignmentService;
            _notificationService = notificationService;
            _driverAssignmentService = driverAssignmentService;
        }

        public async Task ExpireAssignmentsAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var expirationTime = DateTime.UtcNow.AddMinutes(-_options.AssignmentExpirationMinutes);

            _logger.LogInformation(
                "Checking for expired dispatch assignments. Cutoff: {CutoffTime:yyyy-MM-dd HH:mm:ss UTC} (Threshold: {Minutes} min).",
                expirationTime,
                _options.AssignmentExpirationMinutes);

            var specification = new ExpiredAssignmentsSpecification(expirationTime);

            var assignments = await _assignmentRepository.ListAsync(specification, cancellationToken);

            if (assignments.Count == 0)
            {
                _logger.LogInformation(
                    "No expired dispatch assignments found. Check completed in {ElapsedMilliseconds} ms.",
                    stopwatch.ElapsedMilliseconds);
                return;
            }

            _logger.LogInformation(
                "Found {Count} expired dispatch assignment(s) to process.",
                assignments.Count);

            var realtimeNotifications = new List<(Guid UserId, string Title, string Message)>();
            var now = DateTime.UtcNow;
            var reassignedCount = 0;

            foreach (var assignment in assignments)
            {
                assignment.Status = AssignmentStatus.Expired;
                assignment.RespondedAt = now;

                _assignmentRepository.Update(assignment);

                var shipment = assignment.Shipment;

                _logger.LogInformation(
                    "Assignment {AssignmentId} for shipment {TrackingNumber} (Driver: {DriverId}, Attempt #{AttemptNumber}) marked as Expired.",
                    assignment.Id,
                    shipment.TrackingNumber,
                    assignment.DriverId,
                    assignment.AttemptNumber);

                var driver = await _driverAssignmentService.FindBestAvailableDriverAsync(
                    shipment,
                    cancellationToken);

                if (driver is null)
                {
                    _logger.LogWarning(
                        "No alternative driver found for shipment {TrackingNumber} after assignment {AssignmentId} expired.",
                        shipment.TrackingNumber,
                        assignment.Id);

                    continue;
                }

                await _dispatchAssignmentService.CreateAssignmentAsync(shipment, driver, cancellationToken);
                reassignedCount++;

                realtimeNotifications.Add((driver.UserId,
                    "New Shipment Assignment",
                    $"You have received a new shipment assignment for shipment {shipment.TrackingNumber}."
                ));

                _logger.LogInformation(
                    "Shipment {TrackingNumber} successfully reassigned to driver {DriverId}.",
                    shipment.TrackingNumber,
                    driver.Id);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var notification in realtimeNotifications)
            {
                await _notificationService.SendRealtimeAsync(
                    notification.UserId,
                    notification.Title,
                    notification.Message,
                    cancellationToken);
            }

            stopwatch.Stop();

            _logger.LogInformation(
                "Finished processing expired assignments: {ExpiredCount} expired, {ReassignedCount} reassigned in {ElapsedMilliseconds} ms.",
                assignments.Count,
                reassignedCount,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
