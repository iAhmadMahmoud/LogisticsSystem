using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Vehicles.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Exceptions;
using MediatR;

namespace LogisticsSystem.Application.Features.Vehicles.Commands.DeleteVehicle
{
    public sealed class DeleteVehicleCommandHandler : IRequestHandler<DeleteVehicleCommand>
    {
        private readonly IGenericRepository<Vehicle> _vehicleRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteVehicleCommandHandler(
            IGenericRepository<Vehicle> vehicleRepository,
            IUnitOfWork unitOfWork)
        {
            _vehicleRepository = vehicleRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteVehicleCommand request, CancellationToken cancellationToken)
        {
            var vehicle = await _vehicleRepository.FirstOrDefaultAsync(
                new VehicleByIdWithDriverSpecification(request.Id),
                cancellationToken);

            if (vehicle is null)
            {
                throw new KeyNotFoundException("Vehicle not found.");
            }

            if (vehicle.Driver != null)
            {
                throw new DomainException("Cannot delete a vehicle that is currently assigned to a driver.");
            }

            _vehicleRepository.Delete(vehicle);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
