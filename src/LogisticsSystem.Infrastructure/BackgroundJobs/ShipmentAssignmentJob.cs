using Hangfire;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace LogisticsSystem.Infrastructure.BackgroundJobs
{
    public sealed class ShipmentAssignmentJob
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IDriverAssignmentService _driverAssignmentService;
        private readonly IDispatchAssignmentService _dispatchAssignmentService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ShipmentAssignmentJob> _logger;

        public ShipmentAssignmentJob(
            IGenericRepository<Shipment> shipmentRepository,
            IDriverAssignmentService driverAssignmentService,
            IDispatchAssignmentService dispatchAssignmentService,
            ILogger<ShipmentAssignmentJob> logger,
            IUnitOfWork unitOfWork)
        {
            _shipmentRepository = shipmentRepository;
            _driverAssignmentService = driverAssignmentService;
            _dispatchAssignmentService = dispatchAssignmentService;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        [AutomaticRetry(Attempts = 3)]
        public async Task AssignShipmentAsync(
            Guid shipmentId,
            CancellationToken cancellationToken = default)
        {
            var shipment = await _shipmentRepository.GetByIdAsync(
                shipmentId,
                cancellationToken);

            if (shipment is null)
            {
                _logger.LogWarning(
                    "Shipment {ShipmentId} was not found.",
                    shipmentId);

                return;
            }

            var driver = await _driverAssignmentService
                .FindBestAvailableDriverAsync(
                    shipment,
                    cancellationToken);

            if (driver is null)
            {
                _logger.LogWarning(
                    "No available driver found for shipment {ShipmentId}.",
                    shipmentId);

                return;
            }

            await _dispatchAssignmentService.CreateAssignmentAsync(
                shipment,
                driver,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "Shipment {ShipmentId} assigned to driver {DriverId}.",
                shipment.Id,
                driver.Id);
        }
    }
}