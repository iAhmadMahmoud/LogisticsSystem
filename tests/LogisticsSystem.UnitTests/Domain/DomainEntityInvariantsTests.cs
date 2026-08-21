using FluentAssertions;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using Xunit;

namespace LogisticsSystem.UnitTests.Domain
{
    public class DomainEntityInvariantsTests
    {
        [Fact]
        public void Shipment_DefaultsAndProperties_SetCorrectly()
        {
            var id = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var shipment = new Shipment
            {
                Id = id,
                CustomerId = customerId,
                TrackingNumber = "TRK-INVARIANT-001",
                Status = ShipmentStatus.Pending,
                Priority = ShipmentPriority.Express,
                Weight = 15.5m,
                DistanceKm = 45.2m,
                ShippingCost = 120.0m,
                PickupAddress = "Origin St",
                DeliveryAddress = "Dest Ave",
                PickupLatitude = 30.0,
                PickupLongitude = 31.0,
                DeliveryLatitude = 30.5,
                DeliveryLongitude = 31.5
            };

            shipment.Id.Should().Be(id);
            shipment.CustomerId.Should().Be(customerId);
            shipment.TrackingNumber.Should().Be("TRK-INVARIANT-001");
            shipment.Status.Should().Be(ShipmentStatus.Pending);
            shipment.Priority.Should().Be(ShipmentPriority.Express);
            shipment.Weight.Should().Be(15.5m);
            shipment.DistanceKm.Should().Be(45.2m);
            shipment.ShippingCost.Should().Be(120.0m);
        }

        [Fact]
        public void Driver_DefaultsAndProperties_SetCorrectly()
        {
            var driverId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var vehicleId = Guid.NewGuid();

            var driver = new Driver
            {
                Id = driverId,
                UserId = userId,
                VehicleId = vehicleId,
                LicenseNumber = "DL-INVARIANT-999",
                Status = DriverStatus.Available,
                Latitude = 30.1234,
                Longitude = 31.5678
            };

            driver.Id.Should().Be(driverId);
            driver.UserId.Should().Be(userId);
            driver.VehicleId.Should().Be(vehicleId);
            driver.LicenseNumber.Should().Be("DL-INVARIANT-999");
            driver.Status.Should().Be(DriverStatus.Available);
            driver.Latitude.Should().Be(30.1234);
            driver.Longitude.Should().Be(31.5678);
        }

        [Fact]
        public void Vehicle_DefaultsAndProperties_SetCorrectly()
        {
            var vehicleId = Guid.NewGuid();
            var vehicle = new Vehicle
            {
                Id = vehicleId,
                PlateNumber = "ABC-1234",
                Brand = "Mercedes",
                Model = "Sprinter",
                ManufacturingYear = 2024,
                Color = "Silver",
                Type = VehicleType.Van,
                Capacity = 3500m,
                IsActive = true
            };

            vehicle.Id.Should().Be(vehicleId);
            vehicle.PlateNumber.Should().Be("ABC-1234");
            vehicle.Brand.Should().Be("Mercedes");
            vehicle.Model.Should().Be("Sprinter");
            vehicle.ManufacturingYear.Should().Be(2024);
            vehicle.Type.Should().Be(VehicleType.Van);
            vehicle.Capacity.Should().Be(3500m);
            vehicle.IsActive.Should().BeTrue();
        }

        [Fact]
        public void DispatchAssignment_Properties_SetCorrectly()
        {
            var id = Guid.NewGuid();
            var shipmentId = Guid.NewGuid();
            var driverId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var assignment = new DispatchAssignment
            {
                Id = id,
                ShipmentId = shipmentId,
                DriverId = driverId,
                AttemptNumber = 1,
                Status = AssignmentStatus.Pending,
                SentAt = now
            };

            assignment.Id.Should().Be(id);
            assignment.ShipmentId.Should().Be(shipmentId);
            assignment.DriverId.Should().Be(driverId);
            assignment.AttemptNumber.Should().Be(1);
            assignment.Status.Should().Be(AssignmentStatus.Pending);
            assignment.SentAt.Should().Be(now);
        }

        [Fact]
        public void Notification_Properties_SetCorrectly()
        {
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var notification = new Notification
            {
                Id = id,
                UserId = userId,
                Title = "Alert",
                Message = "Test alert content",
                Type = NotificationType.ShipmentAssigned,
                IsRead = false
            };

            notification.Id.Should().Be(id);
            notification.UserId.Should().Be(userId);
            notification.Title.Should().Be("Alert");
            notification.Type.Should().Be(NotificationType.ShipmentAssigned);
            notification.IsRead.Should().BeFalse();
        }
    }
}
