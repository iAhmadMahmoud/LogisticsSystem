using AutoMapper;
using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.ShipmentStatusHistories.DTOs;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogisticsSystem.Application.Features.ShipmentStatusHistories.Queries.GetShipmentStatusHistory
{
    public sealed class GetShipmentStatusHistoryQueryHandler : IRequestHandler<GetShipmentStatusHistoryQuery,IReadOnlyList<ShipmentStatusHistoryDto>>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IGenericRepository<ShipmentStatusHistory>
            _statusHistoryRepository;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly IGenericRepository<Driver> _driverRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetShipmentStatusHistoryQueryHandler(
            IGenericRepository<Shipment> shipmentRepository,
            IGenericRepository<ShipmentStatusHistory>
                statusHistoryRepository,
            IGenericRepository<Customer> customerRepository,
            IGenericRepository<Driver> driverRepository,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _shipmentRepository = shipmentRepository;
            _statusHistoryRepository = statusHistoryRepository;
            _customerRepository = customerRepository;
            _driverRepository = driverRepository;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<ShipmentStatusHistoryDto>> Handle(
            GetShipmentStatusHistoryQuery request,
            CancellationToken cancellationToken)
        {
            // 1. Get the shipment
            var shipment = await _shipmentRepository.GetByIdAsync(
                request.ShipmentId,
                cancellationToken);

            if (shipment is null)
            {
                throw new KeyNotFoundException(
                    "Shipment not found.");
            }

            // 2. Admin and Dispatcher can view any shipment
            var isAdmin = _currentUserService.IsInRole(
                Roles.Admin);

            var isDispatcher = _currentUserService.IsInRole(
                Roles.Dispatcher);

            if (!isAdmin && !isDispatcher)
            {
                // 3. Customer can view only their own shipment
                if (_currentUserService.IsInRole(
                    Roles.Customer))
                {
                    var customer = await _customerRepository
                        .AsQueryable()
                        .FirstOrDefaultAsync(
                            x => x.UserId ==
                                 _currentUserService.UserId,
                            cancellationToken);

                    if (customer is null)
                    {
                        throw new UnauthorizedAccessException(
                            "Customer profile not found.");
                    }

                    if (shipment.CustomerId != customer.Id)
                    {
                        throw new UnauthorizedAccessException(
                            "You are not authorized to view this shipment status history.");
                    }
                }

                // 4. Driver can view only assigned shipments
                else if (_currentUserService.IsInRole(
                    Roles.Driver))
                {
                    var driver = await _driverRepository
                        .AsQueryable()
                        .FirstOrDefaultAsync(
                            x => x.UserId ==
                                 _currentUserService.UserId,
                            cancellationToken);

                    if (driver is null)
                    {
                        throw new UnauthorizedAccessException(
                            "Driver profile not found.");
                    }

                    if (shipment.DriverId != driver.Id)
                    {
                        throw new UnauthorizedAccessException(
                            "You are not authorized to view this shipment status history.");
                    }
                }

                // 5. Any other role is denied
                else
                {
                    throw new UnauthorizedAccessException(
                        "You are not authorized to view shipment status history.");
                }
            }

            // 6. Get status history in chronological order
            var statusHistory = await _statusHistoryRepository
                .AsQueryable()
                .Where(x => x.ShipmentId == request.ShipmentId)
                .OrderBy(x => x.ChangedAt)
                .ToListAsync(cancellationToken);

            // 7. Map entities to DTOs
            return _mapper.Map<
                IReadOnlyList<ShipmentStatusHistoryDto>
            >(statusHistory);
        }
    }
}
