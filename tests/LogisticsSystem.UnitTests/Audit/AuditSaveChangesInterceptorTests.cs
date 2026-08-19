using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Domain.Common;
using LogisticsSystem.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LogisticsSystem.UnitTests.Audit
{
    public class TestAuditableEntity : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    public class TestDbContext : DbContext
    {
        public DbSet<TestAuditableEntity> TestEntities => Set<TestAuditableEntity>();

        private readonly AuditSaveChangesInterceptor _interceptor;

        public TestDbContext(DbContextOptions<TestDbContext> options, AuditSaveChangesInterceptor interceptor)
            : base(options)
        {
            _interceptor = interceptor;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(_interceptor);
            base.OnConfiguring(optionsBuilder);
        }
    }

    public class AuditSaveChangesInterceptorTests
    {
        [Fact]
        public async Task SavingChanges_WhenEntityAddedWithAuthenticatedUser_SetsCreatedAtAndCreatedBy()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

            var interceptor = new AuditSaveChangesInterceptor(currentUserServiceMock.Object);

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new TestDbContext(options, interceptor);

            var entity = new TestAuditableEntity { Name = "Test Shipment" };

            // Act
            context.TestEntities.Add(entity);
            await context.SaveChangesAsync();

            // Assert
            entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            entity.CreatedBy.Should().Be(userId.ToString());
            entity.UpdatedAt.Should().BeNull();
            entity.UpdatedBy.Should().BeNull();
        }

        [Fact]
        public async Task SavingChanges_WhenEntityModifiedWithAuthenticatedUser_SetsUpdatedAtAndUpdatedBy()
        {
            // Arrange
            var createUserId = Guid.NewGuid();
            var updateUserId = Guid.NewGuid();
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            currentUserServiceMock.Setup(x => x.UserId).Returns(createUserId);

            var interceptor = new AuditSaveChangesInterceptor(currentUserServiceMock.Object);

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new TestDbContext(options, interceptor);

            var entity = new TestAuditableEntity { Name = "Initial Name" };
            context.TestEntities.Add(entity);
            await context.SaveChangesAsync();

            // Change user for update
            currentUserServiceMock.Setup(x => x.UserId).Returns(updateUserId);

            // Act
            entity.Name = "Updated Name";
            context.TestEntities.Update(entity);
            await context.SaveChangesAsync();

            // Assert
            entity.CreatedBy.Should().Be(createUserId.ToString());
            entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            entity.UpdatedBy.Should().Be(updateUserId.ToString());
        }

        [Fact]
        public async Task SavingChanges_WhenUnauthenticated_DoesNotThrowAndSetsCreatedAtOnly()
        {
            // Arrange
            var currentUserServiceMock = new Mock<ICurrentUserService>();
            currentUserServiceMock.Setup(x => x.UserId).Throws(new UnauthorizedAccessException("Unauthenticated"));

            var interceptor = new AuditSaveChangesInterceptor(currentUserServiceMock.Object);

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new TestDbContext(options, interceptor);

            var entity = new TestAuditableEntity { Name = "Public User" };

            // Act
            context.TestEntities.Add(entity);
            var act = async () => await context.SaveChangesAsync();

            // Assert
            await act.Should().NotThrowAsync();
            entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
            entity.CreatedBy.Should().BeNull();
        }
    }
}
