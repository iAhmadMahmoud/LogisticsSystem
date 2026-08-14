using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Infrastructure.Services
{
    public sealed class DispatchAssignmentService : IDispatchAssignmentService
    {
        private readonly IGenericRepository<DispatchAssignment> _assignmentRepository;
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork _unitOfWork;


        public DispatchAssignmentService(IGenericRepository<DispatchAssignment> assignmentRepository, IUnitOfWork unitOfWork, INotificationService notificationService)
        {
            _assignmentRepository = assignmentRepository;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        public async Task<DispatchAssignment?> CreateAssignmentAsync(Shipment shipment, Driver driver, CancellationToken cancellationToken = default)
        {
            var attemptNumber = await _assignmentRepository
                .AsQueryable()
                .CountAsync(x=>x.ShipmentId == shipment.Id,cancellationToken) + 1;

            var assignment = new DispatchAssignment
            {
                ShipmentId = shipment.Id,
                DriverId = driver.Id,
                AttemptNumber = attemptNumber,
                Status = AssignmentStatus.Pending,
                SentAt = DateTime.UtcNow
            };

            await _assignmentRepository.AddAsync(assignment, cancellationToken);


            await _notificationService.CreateAsync(
                driver.UserId,
                "New Shipment Assignment",
                $"You have a new shipment assignment: {shipment.TrackingNumber}.",
                NotificationType.DispatchAssignmentReceived,
                cancellationToken);

            return assignment;
        }
    }
}