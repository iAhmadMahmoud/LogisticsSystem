using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.Drivers.Specifications
{
    public sealed class DriverByUserIdSpecification : BaseSpecification<Driver>
    {
        public DriverByUserIdSpecification(Guid userId) : base(driver=>driver.UserId == userId)
        {
            
        }
    }
}
