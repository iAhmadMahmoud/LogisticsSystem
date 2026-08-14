using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Application.Features.Customers.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using MediatR;

namespace LogisticsSystem.Application.Features.Shipments.Commands.CreateShipment
{
    public sealed class CreateShipmentCommandHandler
        : IRequestHandler<CreateShipmentCommand, Guid>
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly IShipmentAssignmentScheduler _shipmentAssignmentScheduler;

        public CreateShipmentCommandHandler(
            IGenericRepository<Shipment> shipmentRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IGenericRepository<Customer> customerRepository,
            IShipmentAssignmentScheduler shipmentAssignmentScheduler)
        {
            _shipmentRepository = shipmentRepository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _customerRepository = customerRepository;
            _shipmentAssignmentScheduler = shipmentAssignmentScheduler;
        }

        public async Task<Guid> Handle(
            CreateShipmentCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Get the current customer
            var specification = new CustomerByUserIdSpecification(
                _currentUserService.UserId);

            var customer = await _customerRepository
                .FirstOrDefaultAsync(
                    specification,
                    cancellationToken);

            if (customer is null)
            {
                throw new UnauthorizedAccessException(
                    "Customer profile not found.");
            }

            // 2. Get shipment data
            var dto = request.Shipment;

            // 3. Create shipment
            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),

                TrackingNumber = GenerateTrackingNumber(),

                CustomerId = customer.Id,

                PickupAddress = dto.PickupAddress,
                PickupLatitude = dto.PickupLatitude,
                PickupLongitude = dto.PickupLongitude,

                DeliveryAddress = dto.DeliveryAddress,
                DeliveryLatitude = dto.DeliveryLatitude,
                DeliveryLongitude = dto.DeliveryLongitude,

                Weight = dto.Weight,
                DistanceKm = dto.DistanceKm,
                ShippingCost = dto.ShippingCost,

                Priority = dto.Priority,

                Status = ShipmentStatus.Pending,

                Notes = dto.Notes,

                ScheduledAt = dto.ScheduledAt
            };

            // 4. Save shipment
            await _shipmentRepository.AddAsync(
                shipment,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            // 5. Schedule background assignment through Hangfire
            _shipmentAssignmentScheduler.Schedule(
                shipment.Id);

            // 6. Return shipment ID
            return shipment.Id;
        }

        private static string GenerateTrackingNumber()
        {
            return $"TRK-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        }
    }
}