using System.Security.Claims;
using FluentAssertions;
using LogisticsSystem.Application.Authorization;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Infrastructure.Authentication.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace LogisticsSystem.UnitTests.Authorization
{
    public class AuthorizationPolicyTests
    {
        private readonly AuthorizationOptions _options;

        public AuthorizationPolicyTests()
        {
            var services = new ServiceCollection();
            services.AddApplicationAuthorization();
            var serviceProvider = services.BuildServiceProvider();
            _options = serviceProvider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        }

        [Fact]
        public void UserManagePolicy_ShouldRequireAdminRole()
        {
            // Arrange
            var policy = _options.GetPolicy(Policies.UserManage);

            // Assert
            policy.Should().NotBeNull();
            var requirement = policy!.Requirements.OfType<Microsoft.AspNetCore.Authorization.Infrastructure.RolesAuthorizationRequirement>().FirstOrDefault();
            requirement.Should().NotBeNull();
            requirement!.AllowedRoles.Should().Contain(Roles.Admin);
            requirement.AllowedRoles.Should().NotContain(Roles.Dispatcher);
            requirement.AllowedRoles.Should().NotContain(Roles.Customer);
            requirement.AllowedRoles.Should().NotContain(Roles.Driver);
        }

        [Fact]
        public void UserViewPolicy_ShouldRequireAdminRole()
        {
            // Arrange
            var policy = _options.GetPolicy(Policies.UserView);

            // Assert
            policy.Should().NotBeNull();
            var requirement = policy!.Requirements.OfType<Microsoft.AspNetCore.Authorization.Infrastructure.RolesAuthorizationRequirement>().FirstOrDefault();
            requirement.Should().NotBeNull();
            requirement!.AllowedRoles.Should().Contain(Roles.Admin);
        }

        [Fact]
        public void DashboardViewPolicy_ShouldRequireDispatcherOrAdminRole()
        {
            // Arrange
            var policy = _options.GetPolicy(Policies.DashboardView);

            // Assert
            policy.Should().NotBeNull();
            var requirement = policy!.Requirements.OfType<Microsoft.AspNetCore.Authorization.Infrastructure.RolesAuthorizationRequirement>().FirstOrDefault();
            requirement.Should().NotBeNull();
            requirement!.AllowedRoles.Should().Contain(Roles.Admin);
            requirement.AllowedRoles.Should().Contain(Roles.Dispatcher);
            requirement.AllowedRoles.Should().NotContain(Roles.Customer);
            requirement.AllowedRoles.Should().NotContain(Roles.Driver);
        }

        [Fact]
        public void VehicleManagePolicy_ShouldRequireDispatcherOrAdminRole()
        {
            // Arrange
            var policy = _options.GetPolicy(Policies.VehicleManage);

            // Assert
            policy.Should().NotBeNull();
            var requirement = policy!.Requirements.OfType<Microsoft.AspNetCore.Authorization.Infrastructure.RolesAuthorizationRequirement>().FirstOrDefault();
            requirement.Should().NotBeNull();
            requirement!.AllowedRoles.Should().Contain(Roles.Admin);
            requirement.AllowedRoles.Should().Contain(Roles.Dispatcher);
        }

        [Fact]
        public void VehicleViewAllPolicy_ShouldRequireDispatcherOrAdminRole()
        {
            // Arrange
            var policy = _options.GetPolicy(Policies.VehicleViewAll);

            // Assert
            policy.Should().NotBeNull();
            var requirement = policy!.Requirements.OfType<Microsoft.AspNetCore.Authorization.Infrastructure.RolesAuthorizationRequirement>().FirstOrDefault();
            requirement.Should().NotBeNull();
            requirement!.AllowedRoles.Should().Contain(Roles.Admin);
            requirement.AllowedRoles.Should().Contain(Roles.Dispatcher);
        }

        [Fact]
        public void VehicleViewPolicy_ShouldRequireAuthenticatedUser()
        {
            // Arrange
            var policy = _options.GetPolicy(Policies.VehicleView);

            // Assert
            policy.Should().NotBeNull();
            policy!.Requirements.OfType<Microsoft.AspNetCore.Authorization.Infrastructure.DenyAnonymousAuthorizationRequirement>().Should().NotBeNull();
        }
    }
}
