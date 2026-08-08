namespace LogisticsSystem.Infrastructure.BackgroundJobs
{
    public sealed class DispatchOptions
    {
        public int AssignmentExpirationMinutes { get; set; } = 5;
    }
}
