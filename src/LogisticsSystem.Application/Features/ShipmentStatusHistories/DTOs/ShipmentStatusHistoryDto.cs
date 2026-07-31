using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Application.Features.ShipmentStatusHistories.DTOs
{
    public sealed class ShipmentStatusHistoryDto
    {
        public ShipmentStatus Status { get; init; }

        public DateTime ChangedAt { get; init; }

        public Guid? ChangedByUserId { get; init; }
    }
}
