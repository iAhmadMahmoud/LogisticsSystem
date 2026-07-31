using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Customers.Specifications;
using LogisticsSystem.Application.Features.Shipments.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.DeleteShipment
{
    public class DeleteShipmentCommandHandler : IRequestHandler<DeleteShipmentCommand>
    {
        private readonly IGenericRepository<Shipment> _repository;
        private readonly IUnitOfWork _unitOfWork;


        public DeleteShipmentCommandHandler
            (
                IGenericRepository<Shipment> repository,
                IUnitOfWork unitOfWork
            )
        {
            _repository = repository;
            _unitOfWork = unitOfWork;

        }

        public async Task Handle(DeleteShipmentCommand request, CancellationToken cancellationToken)
        {
          

            var shipment = await _repository.GetByIdAsync(request.Id,cancellationToken);

            if (shipment is null)
            {
                throw new KeyNotFoundException("Shipment not found.");
            }

            if (shipment.Status != ShipmentStatus.Pending) 
            {
                throw new InvalidOperationException("Only pending shipments can be deleted.");
            }

            _repository.Delete(shipment);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
