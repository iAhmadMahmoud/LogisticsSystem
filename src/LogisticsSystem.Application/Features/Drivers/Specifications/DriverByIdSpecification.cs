using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.Drivers.Specifications
{
    public sealed class DriverByIdSpecification:BaseSpecification<Driver>
    {
        public DriverByIdSpecification(Guid driverId):base(driver => driver.Id == driverId)
        {
            
        }
    }
}
