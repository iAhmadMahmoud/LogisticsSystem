using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Shipments.Commands.AssignDriver;
using LogisticsSystem.Application.Features.Shipments.Specifications;
using LogisticsSystem.Domain.Entities;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LogisticsSystem.Infrastructure.BackgroundJobs
{
    public sealed class ShipmentAssignmentWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScope;
        private readonly ILogger<ShipmentAssignmentWorker> _logger;

        public ShipmentAssignmentWorker(IServiceScopeFactory serviceScope, ILogger<ShipmentAssignmentWorker> logger)
        {
            _serviceScope = serviceScope;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceScope.CreateScope();

                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var shipmentRepository = scope.ServiceProvider.GetRequiredService<IGenericRepository<Shipment>>();

                var driverAssignmentService = scope.ServiceProvider.GetRequiredService<IDriverAssignmentService>();

                var pendingShipments = await shipmentRepository.ListAsync(new PendingShipmentsSpecification(), stoppingToken);

                foreach (var shipment in pendingShipments)
                {
                    try
                    {
                        var driver = await driverAssignmentService.FindBestAvailableDriverAsync(shipment.PickupLatitude, shipment.PickupLongitude, stoppingToken);
                        if (driver is null)
                            continue;

                        await mediator.Send(new AssignDriverCommand(shipment.Id, driver.Id), stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to assign driver for shipment {ShipmentId}", shipment.Id);
                    }

                }




                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
