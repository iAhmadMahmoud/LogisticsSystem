using AutoMapper;
using AutoMapper.QueryableExtensions;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.Shipments.DTOs;
using LogisticsSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Application.Features.Shipments.Queries.GetAllShipments
{
    public sealed class GetAllShipmentsQueryHandler : IRequestHandler<GetAllShipmentsQuery, PagedResult<ShipmentDto>>
    {
        private readonly IGenericRepository<Shipment> _repository;
        private readonly IMapper _mapper;

        public GetAllShipmentsQueryHandler(IGenericRepository<Shipment> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PagedResult<ShipmentDto>> Handle(GetAllShipmentsQuery request, CancellationToken cancellationToken)
        {
            var query = _repository.AsQueryable(); 

            query = ApplySorting(query, request);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ProjectTo<ShipmentDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return new PagedResult<ShipmentDto>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
        private static IQueryable<Shipment> ApplySorting(IQueryable<Shipment> query, GetAllShipmentsQuery request)
        {
            return (request.SortBy?.ToLower()) switch
            {
                "trackingnumber" => request.Descending
                    ? query.OrderByDescending(x => x.TrackingNumber)
                    : query.OrderBy(x => x.TrackingNumber),

                "weight" => request.Descending
                    ? query.OrderByDescending(x => x.Weight)
                    : query.OrderBy(x => x.Weight),

                "distancekm" => request.Descending
                    ? query.OrderByDescending(x => x.DistanceKm)
                    : query.OrderBy(x => x.DistanceKm),

                "shippingcost" => request.Descending
                    ? query.OrderByDescending(x => x.ShippingCost)
                    : query.OrderBy(x => x.ShippingCost),

                "priority" => request.Descending
                    ? query.OrderByDescending(x => x.Priority)
                    : query.OrderBy(x => x.Priority),

                "status" => request.Descending
                    ? query.OrderByDescending(x => x.Status)
                    : query.OrderBy(x => x.Status),

                "scheduledat" => request.Descending
                    ? query.OrderByDescending(x => x.ScheduledAt)
                    : query.OrderBy(x => x.ScheduledAt),

                _ => request.Descending
                    ? query.OrderByDescending(x => x.ScheduledAt)
                    : query.OrderBy(x => x.ScheduledAt)
            };
        }
    }
}
