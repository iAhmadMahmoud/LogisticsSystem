using LogisticsSystem.Application.Common.Interfaces.Authentication;
using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Features.Shipments.Specifications;
using LogisticsSystem.Domain.Constants;
using LogisticsSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace LogisticsSystem.Infrastructure.SignalR
{
    [Authorize]
    public sealed class TrackingHub : Hub
    {
        private readonly IGenericRepository<Shipment> _shipmentRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<TrackingHub> _logger;

        public TrackingHub(
            IGenericRepository<Shipment> shipmentRepository,
            ICurrentUserService currentUserService,
            ILogger<TrackingHub> logger)
        {
            _shipmentRepository = shipmentRepository;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task SubscribeToShipment(Guid shipmentId)
        {
            await ValidateShipmentAccessAsync(shipmentId);

            var groupName = GetShipmentGroupName(shipmentId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task UnsubscribeFromShipment(Guid shipmentId)
        {
            await ValidateShipmentAccessAsync(shipmentId);

            var groupName = GetShipmentGroupName(shipmentId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        private async Task ValidateShipmentAccessAsync(Guid shipmentId)
        {
            var shipment = await _shipmentRepository.FirstOrDefaultAsync(
                new ShipmentByIdWithCustomerSpecification(shipmentId),
                Context.ConnectionAborted);

            if (shipment is null)
            {
                throw new HubException("Shipment not found.");
            }

            if (_currentUserService.IsInRole(Roles.Customer))
            {
                if (shipment.Customer is null || shipment.Customer.UserId != _currentUserService.UserId)
                {
                    throw new HubException("You are not authorized to track this shipment.");
                }
            }
        }

        private static string GetShipmentGroupName(Guid shipmentId)
        {
            return $"Shipment:{shipmentId}";
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("Tracking SignalR client connected: {UserId}", Context.UserIdentifier);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Tracking SignalR client disconnected: {UserId}", Context.UserIdentifier);
            return base.OnDisconnectedAsync(exception);
        }
    }
}

