using Hangfire;
using LogisticsSystem.Application.Common.Interfaces.Services;

namespace LogisticsSystem.Infrastructure.BackgroundJobs
{
    public sealed class ShipmentAssignmentScheduler
        : IShipmentAssignmentScheduler
    {
        private readonly IBackgroundJobClient _backgroundJobClient;

        public ShipmentAssignmentScheduler(
            IBackgroundJobClient backgroundJobClient)
        {
            _backgroundJobClient = backgroundJobClient;
        }

        public void Schedule(Guid shipmentId)
        {
            _backgroundJobClient.Enqueue<ShipmentAssignmentJob>(
                job => job.AssignShipmentAsync(
                    shipmentId,
                    CancellationToken.None));
        }
    }
}