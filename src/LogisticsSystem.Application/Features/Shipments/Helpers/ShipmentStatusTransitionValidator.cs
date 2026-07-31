using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Application.Features.Shipments.Helpers
{
    public static class ShipmentStatusTransitionValidator
    {
        public static bool CanTransition(ShipmentStatus current,ShipmentStatus next)
        {
            return (current, next) switch
            {
                (ShipmentStatus.Pending, ShipmentStatus.Cancelled) => true,
                (ShipmentStatus.Pending, ShipmentStatus.Assigned) => true,
                (ShipmentStatus.Assigned, ShipmentStatus.Cancelled) => true,

                (ShipmentStatus.Assigned, ShipmentStatus.PickedUp) => true,
                
                (ShipmentStatus.PickedUp, ShipmentStatus.InTransit) => true,
                
                (ShipmentStatus.InTransit, ShipmentStatus.Delivered) => true,
                (ShipmentStatus.InTransit, ShipmentStatus.Failed) => true,

                _ => false
            };
        }
    }
}
