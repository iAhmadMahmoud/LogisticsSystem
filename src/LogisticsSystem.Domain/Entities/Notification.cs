using LogisticsSystem.Domain.Common;
using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Domain.Entities
{
    public class Notification : AuditableEntity
    {
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
    }
}
