using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Application.Features.Vehicles.Queries.GetAvailableVehicles;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.Vehicles.Specifications
{
    public class AvailableVehiclesSpecification : BaseSpecification<Vehicle>
    {
        public AvailableVehiclesSpecification(GetAvailableVehiclesQuery query, bool isPaging = true)
        {
            // 1. Vehicle must be active
            AddCriteria(v => v.IsActive);

            // 2. Vehicle must not be assigned to a driver
            AddCriteria(v => v.Driver == null);

            // 3. Optional Type filter
            if (query.Type.HasValue)
            {
                AddCriteria(v => v.Type == query.Type.Value);
            }

            // Include Driver navigation property
            AddInclude(v => v.Driver!);

            // 4. Ordering
            ApplyOrderBy(v => v.PlateNumber);

            // 5. Pagination
            if (isPaging)
            {
                ApplyPaging((query.PageNumber - 1) * query.PageSize, query.PageSize);
            }
        }
    }
}
