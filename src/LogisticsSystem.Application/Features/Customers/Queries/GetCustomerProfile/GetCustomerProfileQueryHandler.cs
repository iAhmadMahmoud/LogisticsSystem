using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Customers.DTOs;
using LogisticsSystem.Application.Features.Customers.Specifications;
using LogisticsSystem.Domain.Entities;
using MediatR;

namespace LogisticsSystem.Application.Features.Customers.Queries.GetCustomerProfile
{
    public sealed class GetCustomerProfileQueryHandler : IRequestHandler<GetCustomerProfileQuery, CustomerProfileDto>
    {
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly ICurrentUserService _currentUserService;

        public GetCustomerProfileQueryHandler(
            IGenericRepository<Customer> customerRepository,
            ICurrentUserService currentUserService)
        {
            _customerRepository = customerRepository;
            _currentUserService = currentUserService;
        }

        public async Task<CustomerProfileDto> Handle(GetCustomerProfileQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            var specification = new CustomerByUserIdSpecification(userId);

            var customer = await _customerRepository.FirstOrDefaultAsync(specification, cancellationToken);

            if (customer is null)
            {
                throw new KeyNotFoundException("Customer profile not found.");
            }

            return new CustomerProfileDto
            {
                Id = customer.Id,
                UserId = customer.UserId,
                DefaultAddress = customer.DefaultAddress,
                CreatedAt = customer.CreatedAt
            };
        }
    }
}
