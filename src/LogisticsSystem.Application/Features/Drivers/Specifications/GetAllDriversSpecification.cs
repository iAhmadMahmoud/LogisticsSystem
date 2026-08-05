using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Application.Features.Drivers.Specifications
{
    public sealed class GetAllDriversSpecification : BaseSpecification<Driver>
    {
        public GetAllDriversSpecification(
            int pageNumber,
            int pageSize,
            DriverStatus? status) : base(driver => !status.HasValue || driver.Status == status.Value)
        {
            ApplyOrderBy(driver=>driver.CreatedAt);

            ApplyPaging((pageNumber - 1) * pageSize, pageSize);
        }
    }
}
