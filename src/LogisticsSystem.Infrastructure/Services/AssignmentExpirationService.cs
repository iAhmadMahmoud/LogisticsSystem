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
        private readonly IDispatchAssignmentService _dispatchAssignmentService;
        private readonly INotificationService _notificationService;
        private readonly IDriverAssignmentService _driverAssignmentService;

        public AssignmentExpirationService(IGenericRepository<DispatchAssignment> assignmentRepository, IUnitOfWork unitOfWork, IOptions<DispatchOptions> options, ILogger<AssignmentExpirationService> logger, IDispatchAssignmentService dispatchAssignmentService, INotificationService notificationService,  IDriverAssignmentService driverAssignmentService)
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
            var expirationTime = DateTime.UtcNow.AddMinutes(-_options.AssignmentExpirationMinutes);

            var specification = new ExpiredAssignmentsSpecification(expirationTime);

            var assignments = await _assignmentRepository.ListAsync(specification, cancellationToken);

            if (assignments.Count == 0)
                return;


            var realtimeNotifications = new List<(Guid UserId, string Title, string Message)>();

            var now = DateTime.UtcNow;

            foreach (var assignment in assignments)
            {
                assignment.Status = AssignmentStatus.Expired;
                assignment.RespondedAt = now;

                _assignmentRepository.Update(assignment);

                var shipment = assignment.Shipment;

                var driver = await _driverAssignmentService
     .FindBestAvailableDriverAsync(
         shipment,
         cancellationToken);

                if (driver is null)
                {
                    _logger.LogWarning(
                        "No available driver found for shipment {ShipmentId} after assignment {AssignmentId} expired.",
                        shipment.Id,
                        assignment.Id);

                    continue;
                }

                await _dispatchAssignmentService.CreateAssignmentAsync(shipment, driver, cancellationToken);

                realtimeNotifications.Add((driver.UserId,
                    "New Shipment Assignment",
                    $"You have received a new shipment assignment for shipment {shipment.TrackingNumber}."
                ));

                _logger.LogInformation(
                    "Shipment {ShipmentId} reassigned to driver {DriverId} after assignment {AssignmentId} expired.",
                    shipment.Id,
                    driver.Id,
                    assignment.Id);
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

            _logger.LogInformation("Expired {Count} dispatch assignments.", assignments.Count);
        }
    }
}
