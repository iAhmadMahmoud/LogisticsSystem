namespace LogisticsSystem.Application.Common.Interfaces.Services
{
    public interface IShipmentAssignmentScheduler
    {
        void Schedule(Guid shipmentId);
    }
}
