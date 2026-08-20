namespace LogisticsSystem.Application.Features.Customers.DTOs
{
    public sealed class CustomerProfileDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? DefaultAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
