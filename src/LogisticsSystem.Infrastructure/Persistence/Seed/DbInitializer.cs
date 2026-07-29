using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using LogisticsSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Infrastructure.Persistence.Seed
{
    public class DbInitializer
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public DbInitializer(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task InitializeAsync()
        {
            // 1. Apply pending migrations
            if (_context.Database.IsSqlServer())
            {
                await _context.Database.MigrateAsync();
            }

            // 2. Seed Identity Roles if they don't exist
            await SeedRolesAsync();

            // 3. Seed Users & Domain Entities
            await SeedUsersAndEntitiesAsync();
        }

        private async Task SeedRolesAsync()
        {
            string[] roles = [Roles.Admin, Roles.Customer, Roles.Driver, Roles.Dispatcher];

            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                }
            }
        }

        private async Task SeedUsersAndEntitiesAsync()
        {
            // Seed Admin User
            var adminUser = await SeedUserAsync("admin@logistics.com", "Admin@123", "Admin", "User", Roles.Admin);

            // Seed Dispatcher User
            await SeedUserAsync("dispatcher@logistics.com", "Dispatcher@123", "Dispatcher", "User", Roles.Dispatcher);

            // Seed Customer User & Entity
            var customerUser = await SeedUserAsync("customer@logistics.com", "Customer@123", "Customer", "User", Roles.Customer);
            if (customerUser != null)
            {
                var existingCustomer = await _context.Customers.AnyAsync(c => c.UserId == customerUser.Id);
                if (!existingCustomer)
                {
                    var customer = new Customer
                    {
                        UserId = customerUser.Id,
                        DefaultAddress = "123 Main Street, Logistics City"
                    };

                    await _context.Customers.AddAsync(customer);
                    await _context.SaveChangesAsync();
                }
            }

            // Seed Driver User, Vehicle & Driver Entity
            var driverUser = await SeedUserAsync("driver@logistics.com", "Driver@123", "Driver", "User", Roles.Driver);
            if (driverUser != null)
            {
                var existingDriver = await _context.Drivers.AnyAsync(d => d.UserId == driverUser.Id);
                if (!existingDriver)
                {
                    var vehicle = new Vehicle
                    {
                        PlateNumber = "LOG-1024",
                        Brand = "Volvo",
                        Model = "FH16",
                        ManufacturingYear = 2024,
                        Color = "White",
                        Type = VehicleType.Truck,
                        Capacity = 20000m,
                        IsActive = true
                    };

                    await _context.Vehicles.AddAsync(vehicle);
                    await _context.SaveChangesAsync();

                    var driver = new Driver
                    {
                        UserId = driverUser.Id,
                        LicenseNumber = "DL-99887766",
                        Status = DriverStatus.Available,
                        VehicleId = vehicle.Id
                    };

                    await _context.Drivers.AddAsync(driver);
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task<ApplicationUser?> SeedUserAsync(string email, string password, string firstName, string lastName, string role)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, password);
                if (createResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, role);
                }
                else
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to seed user '{email}': {errors}");
                }
            }
            else
            {
                if (!await _userManager.IsInRoleAsync(user, role))
                {
                    await _userManager.AddToRoleAsync(user, role);
                }
            }

            return user;
        }
    }
}
