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
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;


        public DeleteShipmentCommandHandler
            (
                IGenericRepository<Shipment> repository,
                IUnitOfWork unitOfWork
,
                IGenericRepository<Customer> customerRepository,
                ICurrentUserService currentUserService)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _customerRepository = customerRepository;
            _currentUserService = currentUserService;
        }

        public async Task Handle(DeleteShipmentCommand request, CancellationToken cancellationToken)
        {

            var customer = await _customerRepository.FirstOrDefaultAsync(new CustomerByUserIdSpecification(_currentUserService.UserId), cancellationToken);

            if (customer is null)
            {
                throw new UnauthorizedAccessException("Customer profile not found.");
            }



            var shipment = await _repository.FirstOrDefaultAsync(new ShipmentByIdAndCustomerSpecification(request.Id, customer.Id), cancellationToken);

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
