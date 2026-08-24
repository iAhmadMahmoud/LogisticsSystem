using FluentAssertions;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Application.Features.Dashboard.Queries.GetDriverDashboardMetrics;
using LogisticsSystem.Application.Features.Dashboard.Queries.GetShipmentDashboardMetrics;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.Persistence.Specifications;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace LogisticsSystem.UnitTests.Performance
{
    public class QueryPerformanceTests
    {
        private class TestEntity
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public string Name { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public List<TestChildEntity> Children { get; set; } = new();
        }

        private class TestChildEntity
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public Guid ParentId { get; set; }
            public string Title { get; set; } = string.Empty;
        }

        private class TestSpecification : BaseSpecification<TestEntity>
        {
            public TestSpecification(string nameFilter)
                : base(x => x.Name.Contains(nameFilter))
            {
                AddInclude(x => x.Children);
                ApplyOrderByDescending(x => x.CreatedAt);
                ApplyPaging(0, 10);
            }

            public void SetTracking(bool tracking) => AsNoTracking(!tracking);
            public void SetSplit(bool split) => AsSplitQuery(split);
        }

        [Fact]
        public void BaseSpecification_DefaultConfiguration_HasIsNoTrackingTrueAndIsSplitQueryFalse()
        {
            // Arrange & Act
            var spec = new TestSpecification("test");

            // Assert
            spec.IsNoTracking.Should().BeTrue();
            spec.IsSplitQuery.Should().BeFalse();
            spec.IsPagingEnabled.Should().BeTrue();
            spec.Take.Should().Be(10);
            spec.Skip.Should().Be(0);
        }

        [Fact]
        public void BaseSpecification_WhenConfigured_TogglesNoTrackingAndSplitQuery()
        {
            // Arrange
            var spec = new TestSpecification("test");

            // Act
            spec.SetTracking(true);
            spec.SetSplit(true);

            // Assert
            spec.IsNoTracking.Should().BeFalse();
            spec.IsSplitQuery.Should().BeTrue();
        }

        [Fact]
        public void SpecificationEvaluator_WhenEvaluatingCount_FiltersCriteriaWithoutApplyingPaging()
        {
            // Arrange
            var data = new List<TestEntity>
            {
                new() { Name = "Alpha", CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                new() { Name = "Beta", CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
                new() { Name = "Alpha 2", CreatedAt = DateTime.UtcNow }
            }.AsQueryable();

            var spec = new TestSpecification("Alpha");

            // Act - Evaluate for Count (evaluatePaging = false)
            var countQuery = SpecificationEvaluator<TestEntity>.GetQuery(data, spec, evaluatePaging: false);
            var resultCount = countQuery.Count();

            // Assert
            resultCount.Should().Be(2);
        }

        [Fact]
        public void SpecificationEvaluator_WhenEvaluatingPaging_AppliesPagingAndCriteria()
        {
            // Arrange
            var data = new List<TestEntity>
            {
                new() { Name = "Alpha 1", CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                new() { Name = "Alpha 2", CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
                new() { Name = "Alpha 3", CreatedAt = DateTime.UtcNow }
            }.AsQueryable();

            var spec = new TestSpecification("Alpha");

            // Act - Evaluate for List (evaluatePaging = true)
            var listQuery = SpecificationEvaluator<TestEntity>.GetQuery(data, spec, evaluatePaging: true);
            var results = listQuery.ToList();

            // Assert
            results.Should().HaveCount(3);
            results[0].Name.Should().Be("Alpha 3"); // Ordered descending by CreatedAt
        }
    }
}
