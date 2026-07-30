using AutoMapper;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Models;
using LogisticsSystem.Application.Features.ShipmentTrackings.DTOs;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Application.Features.ShipmentTrackings.Queries.GetShipmentTracking
{
    public sealed class GetShipmentTrackingQueryHandler : IRequestHandler<GetShipmentTrackingQuery, PagedResult<ShipmentTrackingDto>>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IGenericRepository<ShipmentTracking> _trackingRepository;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        public GetShipmentTrackingQueryHandler(
            IGenericRepository<Shipment> shipmentRepository,
            IGenericRepository<ShipmentTracking> trackingRepository,
            IGenericRepository<Customer> customerRepository,
            IGenericRepository<Driver> driverRepository,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _shipmentRepository = shipmentRepository;
            _trackingRepository = trackingRepository;
            _customerRepository = customerRepository;
            _driverRepository = driverRepository;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<PagedResult<ShipmentTrackingDto>> Handle(GetShipmentTrackingQuery request, CancellationToken cancellationToken)
        {
            // 1. Validate pagination values
            var pageNumber = request.PageNumber < 1
                ? 1
                : request.PageNumber;

            var pageSize = request.PageSize < 1
                ? 20
                : request.PageSize;

            // Optional protection against very large requests
            pageSize = Math.Min(pageSize, 100);

            //2. Get the shipment
            var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);

            if(shipment is null)
            {
                throw new KeyNotFoundException("Shipment not found.");
            }

            //3. Admin and Dispatcher can view any shipment
            var isAdmin = _currentUserService.IsInRole(Roles.Admin);
            var isDispatcher = _currentUserService.IsInRole(Roles.Dispatcher);

            if(!isAdmin  && !isDispatcher)
            {
                //4. Customer can view only their own shipment
                if (_currentUserService.IsInRole(Roles.Customer))
                {
                    var customer = await _customerRepository.AsQueryable().FirstOrDefaultAsync(x=>x.UserId == _currentUserService.UserId,cancellationToken);

                    if (customer is null)
                    {
                        throw new UnauthorizedAccessException(
                            "Customer profile not found.");
                    }

                    if (shipment.CustomerId != customer.Id)
                    {
                        throw new UnauthorizedAccessException(
                            "You are not authorized to view this shipment tracking.");
                    }
                }
                // 5. Driver can view only shipments assigned to them
                else if (_currentUserService.IsInRole(Roles.Driver))
                {
                    var driver = await _driverRepository
                        .AsQueryable()
                        .FirstOrDefaultAsync(
                            x => x.UserId == _currentUserService.UserId,
                            cancellationToken);

                    if (driver is null)
                    {
                        throw new UnauthorizedAccessException(
                            "Driver profile not found.");
                    }

                    if (shipment.DriverId != driver.Id)
                    {
                        throw new UnauthorizedAccessException(
                            "You are not authorized to view this shipment tracking.");
                    }
                }
                // 6. Any other role is denied
                else
                {
                    throw new UnauthorizedAccessException(
                        "You are not authorized to view shipment tracking.");
                }
            }
            // 7. Build the tracking query
            var trackingQuery = _trackingRepository
                .AsQueryable()
                .Where(x => x.ShipmentId == request.ShipmentId)
                .OrderByDescending(x => x.RecordedAt);

            // 8. Get the total count before pagination
            var totalCount = await trackingQuery.CountAsync(
                cancellationToken);

            // 9. Get only the requested page
            var trackingRecords = await trackingQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            // 10. Map entities to DTOs
            var items = _mapper.Map<
                IReadOnlyList<ShipmentTrackingDto>
            >(trackingRecords);

            // 11. Return the paginated result
            return new PagedResult<ShipmentTrackingDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }
}
