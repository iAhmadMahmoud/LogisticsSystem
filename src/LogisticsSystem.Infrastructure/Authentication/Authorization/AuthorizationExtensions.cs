using LogisticsSystem.Application.Authorization;
using LogisticsSystem.Domain.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace LogisticsSystem.Infrastructure.Authentication.Authorization
{
    public static class AuthorizationExtensions
    {
        public static IServiceCollection AddApplicationAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // Shipments
                options.AddPolicy(
                    Policies.ShipmentCreate,
                    policy => policy.RequireRole(
                        Roles.Customer,
                        Roles.Dispatcher,
                        Roles.Admin));

                options.AddPolicy(
                    Policies.ShipmentView,
                    policy => policy.RequireAuthenticatedUser());

                options.AddPolicy(
                    Policies.ShipmentViewAll,
                    policy => policy.RequireRole(
                        Roles.Dispatcher,
                        Roles.Admin));

                options.AddPolicy(
                    Policies.ShipmentUpdate,
                    policy => policy.RequireRole(
                        Roles.Customer,
                        Roles.Dispatcher,
                        Roles.Admin));

                options.AddPolicy(
                    Policies.ShipmentCancel,
                    policy => policy.RequireRole(
                        Roles.Customer,
                        Roles.Dispatcher,
                        Roles.Admin));

                options.AddPolicy(
                    Policies.ShipmentDelete,
                    policy => policy.RequireRole(
                        Roles.Admin));

                // Drivers
                options.AddPolicy(
                    Policies.DriverView,
                    policy => policy.RequireAuthenticatedUser());

                options.AddPolicy(
                    Policies.DriverViewAll,
                    policy => policy.RequireRole(
                        Roles.Dispatcher,
                        Roles.Admin));

                options.AddPolicy(
                    Policies.DriverUpdateStatus,
                    policy => policy.RequireRole(
                        Roles.Driver));

                options.AddPolicy(
                    Policies.DriverManage,
                    policy => policy.RequireRole(
                        Roles.Dispatcher,
                        Roles.Admin));

                // Dispatch

                options.AddPolicy(
                    Policies.DispatchAssignDriver,
                    policy => policy.RequireRole(
                        Roles.Dispatcher,
                        Roles.Admin));

                // Notifications

                options.AddPolicy(
                    Policies.NotificationView,
                    policy => policy.RequireAuthenticatedUser());

                // Users

                options.AddPolicy(
                    Policies.UserView,
                    policy => policy.RequireRole(
                        Roles.Admin));

                options.AddPolicy(
                    Policies.UserManage,
                    policy => policy.RequireRole(
                        Roles.Admin));

                // Dashboard

                options.AddPolicy(
                    Policies.DashboardView,
                    policy => policy.RequireRole(
                        Roles.Dispatcher,
                        Roles.Admin));

                // Vehicles

                options.AddPolicy(
                    Policies.VehicleView,
                    policy => policy.RequireAuthenticatedUser());

                options.AddPolicy(
                    Policies.VehicleViewAll,
                    policy => policy.RequireRole(
                        Roles.Dispatcher,
                        Roles.Admin));

                options.AddPolicy(
                    Policies.VehicleManage,
                    policy => policy.RequireRole(
                        Roles.Dispatcher,
                        Roles.Admin));
            });

            return services;
        }

    }
}
