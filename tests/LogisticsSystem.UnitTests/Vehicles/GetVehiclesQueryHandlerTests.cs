using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Application.Features.Vehicles.Queries.GetVehicles;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Vehicles
{
    public class GetVehiclesQueryHandlerTests
    {
        private readonly Mock<IGenericRepository<Vehicle>> _vehicleRepoMock = new();
        private readonly GetVehiclesQueryHandler _handler;

        public GetVehiclesQueryHandlerTests()
        {
            _handler = new GetVehiclesQueryHandler(_vehicleRepoMock.Object);
        }

        [Fact]
        public async Task Handle_WhenCalled_ReturnsPagedResultOfVehicles()
        {
            // Arrange
            var query = new GetVehiclesQuery(1, 10);
            var vehicles = new List<Vehicle>
            {
                new Vehicle
                {
                    Id = Guid.NewGuid(),
                    PlateNumber = "ABC-1234",
                    Brand = "Mercedes",
                    Model = "Sprinter",
                    Type = VehicleType.Van,
                    IsActive = true
                }
            };

            _vehicleRepoMock.Setup(x => x.CountAsync(
                    It.IsAny<ISpecification<Vehicle>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _vehicleRepoMock.Setup(x => x.ListAsync(
                    It.IsAny<ISpecification<Vehicle>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(vehicles);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(1);
            result.Items.Should().HaveCount(1);
            result.Items[0].PlateNumber.Should().Be("ABC-1234");
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(10);
        }
    }
}
