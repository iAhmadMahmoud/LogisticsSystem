using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.DeleteShipment
{
    public class DeleteShipmentCommandHandler : IRequestHandler<DeleteShipmentCommand>
    {
        private readonly IGenericRepository<Shipment> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteShipmentCommandHandler(IGenericRepository<Shipment> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteShipmentCommand request, CancellationToken cancellationToken)
        {
            var shipment = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (shipment is null)
            {
                throw new KeyNotFoundException("Shipment not found.");
            }

            _repository.Delete(shipment);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
