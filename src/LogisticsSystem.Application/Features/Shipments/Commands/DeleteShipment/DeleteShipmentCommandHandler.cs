using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Customers.Specifications;
using LogisticsSystem.Application.Features.Shipments.Specifications;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.DeleteShipment
{
    public class DeleteShipmentCommandHandler : IRequestHandler<DeleteShipmentCommand>
    {
        private readonly IGenericRepository<Shipment> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly ICurrentUserService _currentUserService;

        public DeleteShipmentCommandHandler
            (
                IGenericRepository<Shipment> repository,
                IUnitOfWork unitOfWork,
                ICurrentUserService currentUserService,
                IGenericRepository<Customer> customerRepository
            )
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _customerRepository = customerRepository;
        }

        public async Task Handle(DeleteShipmentCommand request, CancellationToken cancellationToken)
        {
            var customer = await _customerRepository.FirstOrDefaultAsync(new CustomerByUserIdSpecification(_currentUserService.UserId));
            if (customer is null)
            {
                throw new UnauthorizedAccessException("Customer profile not found.");
            }

            var shipment = await _repository.FirstOrDefaultAsync(new ShipmentByIdAndCustomerSpecification(request.Id, customer.Id));

            if (shipment is null)
            {
                throw new KeyNotFoundException("Shipment not found.");
            }

            _repository.Delete(shipment);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
