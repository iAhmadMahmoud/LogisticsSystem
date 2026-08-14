using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Application.Features.Dispatch.Queries.GetAssignmentHistory
{
    public sealed record AssignmentHistoryResponse(
                Guid AssignmentId,
        Guid ShipmentId,
        Guid DriverId,
        int AttemptNumber,
        AssignmentStatus Status,
        DateTime SentAt,
        DateTime? RespondedAt
        );
}
