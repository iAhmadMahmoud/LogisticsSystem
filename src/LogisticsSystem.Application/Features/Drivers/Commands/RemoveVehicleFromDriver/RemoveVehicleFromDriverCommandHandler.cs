using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Drivers.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Exceptions;
using MediatR;

namespace LogisticsSystem.Application.Features.Drivers.Commands.RemoveVehicleFromDriver
{
    public sealed class RemoveVehicleFromDriverCommandHandler : IRequestHandler<RemoveVehicleFromDriverCommand>
    {
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveVehicleFromDriverCommandHandler(
            IGenericRepository<Driver> driverRepository,
            IUnitOfWork unitOfWork)
        {
            _driverRepository = driverRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(RemoveVehicleFromDriverCommand request, CancellationToken cancellationToken)
        {
            var driver = await _driverRepository.FirstOrDefaultAsync(
                new DriverByIdSpecification(request.DriverId),
                cancellationToken);

            if (driver is null)
            {
                throw new KeyNotFoundException("Driver not found.");
            }

            if (!driver.VehicleId.HasValue)
            {
                throw new DomainException("Driver does not have an assigned vehicle.");
            }

            driver.VehicleId = null;
            _driverRepository.Update(driver);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
