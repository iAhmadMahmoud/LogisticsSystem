using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Application.Features.Dispatch.Queries.GetMyAssignments
{
    public sealed class DispatchAssignmentResponse
    {
        public Guid Id { get; init; }
        public Guid ShipmentId { get; init; }
        public Guid DriverId { get; init; }
        public int AttemptNumber { get; init; }
        public AssignmentStatus Status { get; init; }
        public DateTime SentAt { get; init; }
        public DateTime? RespondedAt { get; init; }
    }
}