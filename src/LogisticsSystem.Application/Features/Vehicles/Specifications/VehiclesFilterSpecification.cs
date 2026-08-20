using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Application.Features.Vehicles.Queries.GetVehicles;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.Vehicles.Specifications
{
    public class VehiclesFilterSpecification : BaseSpecification<Vehicle>
    {
        public VehiclesFilterSpecification(GetVehiclesQuery query, bool isPaging = true)
        {
            // 1. Vehicle Type Filter
            if (query.Type.HasValue)
            {
                AddCriteria(v => v.Type == query.Type.Value);
            }

            // 2. Active Status Filter
            if (query.IsActive.HasValue)
            {
                AddCriteria(v => v.IsActive == query.IsActive.Value);
            }

            // 3. Assignment Filter
            if (query.IsAssigned.HasValue)
            {
                if (query.IsAssigned.Value)
                {
                    AddCriteria(v => v.Driver != null);
                }
                else
                {
                    AddCriteria(v => v.Driver == null);
                }
            }

            // 4. Search Filter (PlateNumber, Brand, Model)
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var term = query.SearchTerm.Trim().ToLower();
                AddCriteria(v =>
                    v.PlateNumber.ToLower().Contains(term) ||
                    v.Brand.ToLower().Contains(term) ||
                    v.Model.ToLower().Contains(term));
            }

            // Include Driver navigation property
            AddInclude(v => v.Driver!);

            // 5. Sorting
            if (isPaging)
            {
                switch (query.SortBy?.ToLower())
                {
                    case "platenumber":
                        if (query.Descending) ApplyOrderByDescending(v => v.PlateNumber);
                        else ApplyOrderBy(v => v.PlateNumber);
                        break;

                    case "brand":
                        if (query.Descending) ApplyOrderByDescending(v => v.Brand);
                        else ApplyOrderBy(v => v.Brand);
                        break;

                    case "model":
                        if (query.Descending) ApplyOrderByDescending(v => v.Model);
                        else ApplyOrderBy(v => v.Model);
                        break;

                    case "capacity":
                        if (query.Descending) ApplyOrderByDescending(v => v.Capacity);
                        else ApplyOrderBy(v => v.Capacity);
                        break;

                    case "manufacturingyear":
                        if (query.Descending) ApplyOrderByDescending(v => v.ManufacturingYear);
                        else ApplyOrderBy(v => v.ManufacturingYear);
                        break;

                    case "createdat":
                    default:
                        if (query.Descending) ApplyOrderByDescending(v => v.CreatedAt);
                        else ApplyOrderBy(v => v.CreatedAt);
                        break;
                }

                // 6. Pagination
                ApplyPaging((query.PageNumber - 1) * query.PageSize, query.PageSize);
            }
        }
    }
}
