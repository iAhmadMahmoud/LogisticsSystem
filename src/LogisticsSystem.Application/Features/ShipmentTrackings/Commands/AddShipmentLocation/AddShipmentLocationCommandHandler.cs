using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Application.Features.ShipmentTrackings.Commands.AddShipmentLocation
{
    public sealed class AddShipmentLocationCommandHandler : IRequestHandler<AddShipmentLocationCommand>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly IGenericRepository<ShipmentTracking> _shipmentTrackingRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public AddShipmentLocationCommandHandler
            (
                IGenericRepository<Shipment> shipmentRepository,
                IGenericRepository<Driver> driverRepository,
                IGenericRepository<ShipmentTracking> shipmentTrackingRepository,
                ICurrentUserService currentUserService,
                IUnitOfWork unitOfWork
            )
        {
            _shipmentRepository = shipmentRepository;
            _driverRepository = driverRepository;
            _shipmentTrackingRepository = shipmentTrackingRepository;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(AddShipmentLocationCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _shipmentRepository.GetByIdAsync( request.ShipmentId ,cancellationToken);
            if (shipment is null)
            {
                throw new KeyNotFoundException("Shipment not found.");
            }

            var driver = await _driverRepository.AsQueryable().FirstOrDefaultAsync(d => d.UserId == _currentUserService.UserId, cancellationToken);

            if (driver is null)
            {
                throw new UnauthorizedAccessException("Driver profile not found.");
            }
            if (shipment.DriverId != driver.Id)
            {
                throw new UnauthorizedAccessException("You are not assigned to this shipment.");
            }

            var tracking = new ShipmentTracking
            {
                ShipmentId = shipment.Id,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                RecordedAt = DateTime.UtcNow
            };

            await _shipmentTrackingRepository.AddAsync(tracking,cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
