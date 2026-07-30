using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Application.Features.Drivers.Specifications
{
    public sealed class AvailableDriversSpecification : BaseSpecification<Driver>
    {
        public AvailableDriversSpecification(): base(d=>d.Status == DriverStatus.Available)
        {
            
        }
    }
}
