using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Application.Features.Shipments.Queries.GetAllShipments;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.Shipments.Specifications
{
    public class ShipmentSpecification : BaseSpecification<Shipment>
    {
        public ShipmentSpecification(GetAllShipmentsQuery query)
        {
            // 1. Search Criteria
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToLower();
                AddCriteria(x =>
                    x.TrackingNumber.ToLower().Contains(search) ||
                    x.PickupAddress.ToLower().Contains(search) ||
                    x.DeliveryAddress.ToLower().Contains(search));
            }

            // 2. Filter Criteria
            if (query.Status.HasValue)
            {
                AddCriteria(x => x.Status == query.Status.Value);
            }

            if (query.Priority.HasValue)
            {
                AddCriteria(x => x.Priority == query.Priority.Value);
            }

            if (query.CustomerId.HasValue)
            {
                AddCriteria(x => x.CustomerId == query.CustomerId.Value);
            }

            if (query.DriverId.HasValue)
            {
                AddCriteria(x => x.DriverId == query.DriverId.Value);
            }

            if (query.ScheduledFrom.HasValue)
            {
                AddCriteria(x => x.ScheduledAt >= query.ScheduledFrom.Value);
            }

            if (query.ScheduledTo.HasValue)
            {
                AddCriteria(x => x.ScheduledAt <= query.ScheduledTo.Value);
            }

            // 3. Sorting
            switch (query.SortBy?.ToLower())
            {
                case "trackingnumber":
                    if (query.Descending) ApplyOrderByDescending(x => x.TrackingNumber);
                    else ApplyOrderBy(x => x.TrackingNumber);
                    break;

                case "weight":
                    if (query.Descending) ApplyOrderByDescending(x => x.Weight);
                    else ApplyOrderBy(x => x.Weight);
                    break;

                case "distancekm":
                    if (query.Descending) ApplyOrderByDescending(x => x.DistanceKm);
                    else ApplyOrderBy(x => x.DistanceKm);
                    break;

                case "shippingcost":
                    if (query.Descending) ApplyOrderByDescending(x => x.ShippingCost);
                    else ApplyOrderBy(x => x.ShippingCost);
                    break;

                case "priority":
                    if (query.Descending) ApplyOrderByDescending(x => x.Priority);
                    else ApplyOrderBy(x => x.Priority);
                    break;

                case "status":
                    if (query.Descending) ApplyOrderByDescending(x => x.Status);
                    else ApplyOrderBy(x => x.Status);
                    break;

                case "scheduledat":
                    if (query.Descending) ApplyOrderByDescending(x => x.ScheduledAt);
                    else ApplyOrderBy(x => x.ScheduledAt);
                    break;

                default:
                    if (query.Descending) ApplyOrderByDescending(x => x.ScheduledAt);
                    else ApplyOrderBy(x => x.ScheduledAt);
                    break;
            }

            // 4. Pagination
            ApplyPaging((query.PageNumber - 1) * query.PageSize, query.PageSize);
        }
    }
}
