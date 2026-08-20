using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Application.Features.Shipments.Queries.GetMyShipments;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.Shipments.Specifications
{
    public class MyShipmentsSpecification : BaseSpecification<Shipment>
    {
        public MyShipmentsSpecification(Guid customerId, GetMyShipmentsQuery query)
        {
            // 1. Mandatory Customer Filter
            AddCriteria(x => x.CustomerId == customerId);

            // 2. Status Filter
            if (query.Status.HasValue)
            {
                AddCriteria(x => x.Status == query.Status.Value);
            }

            // 3. Date Filters (applied to CreatedAt)
            if (query.FromDate.HasValue)
            {
                AddCriteria(x => x.CreatedAt >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                AddCriteria(x => x.CreatedAt <= query.ToDate.Value);
            }

            // 4. Sorting
            switch (query.SortBy?.ToLower())
            {
                case "trackingnumber":
                    if (query.Descending) ApplyOrderByDescending(x => x.TrackingNumber);
                    else ApplyOrderBy(x => x.TrackingNumber);
                    break;

                case "status":
                    if (query.Descending) ApplyOrderByDescending(x => x.Status);
                    else ApplyOrderBy(x => x.Status);
                    break;

                case "shippingcost":
                    if (query.Descending) ApplyOrderByDescending(x => x.ShippingCost);
                    else ApplyOrderBy(x => x.ShippingCost);
                    break;

                case "scheduledat":
                    if (query.Descending) ApplyOrderByDescending(x => x.ScheduledAt);
                    else ApplyOrderBy(x => x.ScheduledAt);
                    break;

                case "createdat":
                default:
                    if (query.Descending) ApplyOrderByDescending(x => x.CreatedAt);
                    else ApplyOrderBy(x => x.CreatedAt);
                    break;
            }

            // 5. Pagination
            ApplyPaging((query.PageNumber - 1) * query.PageSize, query.PageSize);
        }
    }
}
