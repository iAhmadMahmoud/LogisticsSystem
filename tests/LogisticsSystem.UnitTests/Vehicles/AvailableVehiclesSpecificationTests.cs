using FluentAssertions;
using LogisticsSystem.Application.Features.Vehicles.Queries.GetAvailableVehicles;
using LogisticsSystem.Application.Features.Vehicles.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using Xunit;

namespace LogisticsSystem.UnitTests.Vehicles
{
    public class AvailableVehiclesSpecificationTests
    {
        [Fact]
        public void Specification_ShouldFilterActiveAndUnassignedVehiclesOnly()
        {
            // Arrange
            var availableVehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                PlateNumber = "ABC-123",
                IsActive = true,
                Driver = null,
                Type = VehicleType.Car
            };

            var inactiveVehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                PlateNumber = "INACT-001",
                IsActive = false,
                Driver = null,
                Type = VehicleType.Car
            };

            var assignedVehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                PlateNumber = "ASSIGN-001",
                IsActive = true,
                Driver = new Driver { Id = Guid.NewGuid() },
                Type = VehicleType.Car
            };

            var query = new GetAvailableVehiclesQuery(PageNumber: 1, PageSize: 10);
            var spec = new AvailableVehiclesSpecification(query, isPaging: false);

            // Act & Assert
            spec.Criteria.Should().NotBeNull();
            var compiledCriteria = spec.Criteria!.Compile();

            compiledCriteria(availableVehicle).Should().BeTrue();
            compiledCriteria(inactiveVehicle).Should().BeFalse();
            compiledCriteria(assignedVehicle).Should().BeFalse();
        }

        [Fact]
        public void Specification_WithTypeFilter_ShouldFilterCorrectVehicleType()
        {
            // Arrange
            var truckVehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                PlateNumber = "TRK-001",
                IsActive = true,
                Driver = null,
                Type = VehicleType.Truck
            };

            var vanVehicle = new Vehicle
            {
                Id = Guid.NewGuid(),
                PlateNumber = "VAN-001",
                IsActive = true,
                Driver = null,
                Type = VehicleType.Van
            };

            var query = new GetAvailableVehiclesQuery(PageNumber: 1, PageSize: 10, Type: VehicleType.Truck);
            var spec = new AvailableVehiclesSpecification(query, isPaging: false);

            // Act & Assert
            spec.Criteria.Should().NotBeNull();
            var compiledCriteria = spec.Criteria!.Compile();

            compiledCriteria(truckVehicle).Should().BeTrue();
            compiledCriteria(vanVehicle).Should().BeFalse();
        }

        [Fact]
        public void Specification_WithPaging_ShouldSetSkipAndTake()
        {
            // Arrange
            var query = new GetAvailableVehiclesQuery(PageNumber: 3, PageSize: 15);

            // Act
            var spec = new AvailableVehiclesSpecification(query, isPaging: true);

            // Assert
            spec.IsPagingEnabled.Should().BeTrue();
            spec.Skip.Should().Be(30);
            spec.Take.Should().Be(15);
        }
    }
}
