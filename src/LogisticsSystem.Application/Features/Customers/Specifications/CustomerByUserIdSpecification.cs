using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.Customers.Specifications
{
    public sealed class CustomerByUserIdSpecification : BaseSpecification<Customer>
    {
        public CustomerByUserIdSpecification(Guid userId) : base(c=>c.UserId == userId)
        {
        }
    }
}
