using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Customers.Specifications;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Customers.Commands.UpdateCustomerProfile
{
    public sealed class UpdateCustomerProfileCommandHandler : IRequestHandler<UpdateCustomerProfileCommand>
    {
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IIdentityService _identityService;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateCustomerProfileCommandHandler(
            IGenericRepository<Customer> customerRepository,
            ICurrentUserService currentUserService,
            IIdentityService identityService,
            IUnitOfWork unitOfWork)
        {
            _customerRepository = customerRepository;
            _currentUserService = currentUserService;
            _identityService = identityService;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(UpdateCustomerProfileCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            var specification = new CustomerByUserIdSpecification(userId);

            var customer = await _customerRepository.FirstOrDefaultAsync(specification, cancellationToken);

            if (customer is null)
            {
                throw new KeyNotFoundException("Customer profile not found.");
            }

            customer.DefaultAddress = request.DefaultAddress;
            _customerRepository.Update(customer);

            await _identityService.UpdateProfileAsync(
                userId,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
