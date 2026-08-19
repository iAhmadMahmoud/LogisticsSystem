using FluentAssertions;
using LogisticsSystem.Application.Features.Shipments.Helpers;
using LogisticsSystem.Domain.Enums;
using Xunit;

namespace LogisticsSystem.UnitTests.Domain
{
    public class ShipmentStatusTransitionValidatorTests
    {
        [Theory]
        [InlineData(ShipmentStatus.Pending, ShipmentStatus.Assigned, true)]
        [InlineData(ShipmentStatus.Pending, ShipmentStatus.Cancelled, true)]
        [InlineData(ShipmentStatus.Assigned, ShipmentStatus.PickedUp, true)]
        [InlineData(ShipmentStatus.Assigned, ShipmentStatus.Cancelled, true)]
        [InlineData(ShipmentStatus.PickedUp, ShipmentStatus.InTransit, true)]
        [InlineData(ShipmentStatus.InTransit, ShipmentStatus.Delivered, true)]
        [InlineData(ShipmentStatus.InTransit, ShipmentStatus.Failed, true)]
        public void CanTransition_ShouldReturnTrue_ForValidTransitions(
            ShipmentStatus currentStatus,
            ShipmentStatus targetStatus,
            bool expected)
        {
            // Act
            var result = ShipmentStatusTransitionValidator.CanTransition(currentStatus, targetStatus);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(ShipmentStatus.Delivered, ShipmentStatus.Cancelled)]
        [InlineData(ShipmentStatus.Delivered, ShipmentStatus.InTransit)]
        [InlineData(ShipmentStatus.Cancelled, ShipmentStatus.InTransit)]
        [InlineData(ShipmentStatus.Failed, ShipmentStatus.Delivered)]
        [InlineData(ShipmentStatus.Pending, ShipmentStatus.Delivered)]
        [InlineData(ShipmentStatus.PickedUp, ShipmentStatus.Delivered)]
        public void CanTransition_ShouldReturnFalse_ForInvalidTransitions(
            ShipmentStatus currentStatus,
            ShipmentStatus targetStatus)
        {
            // Act
            var result = ShipmentStatusTransitionValidator.CanTransition(currentStatus, targetStatus);

            // Assert
            result.Should().BeFalse();
        }
    }
}
