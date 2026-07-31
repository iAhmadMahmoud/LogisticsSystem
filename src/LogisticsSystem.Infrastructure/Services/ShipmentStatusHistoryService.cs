using LogisticsSystem.Application.Common.Interfaces.Persistence;
using LogisticsSystem.Application.Common.Interfaces.Services;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogisticsSystem.Infrastructure.Services
{
    public sealed class ShipmentStatusHistoryService : IShipmentStatusHistoryService
    {
        private readonly IGenericRepository<ShipmentStatusHistory> _statusHistoryRepository;

        public ShipmentStatusHistoryService(IGenericRepository<ShipmentStatusHistory> statusHistoryRepository)
        {
            _statusHistoryRepository = statusHistoryRepository;
        }

        public async Task AddAsync(Shipment shipment, ShipmentStatus status, Guid? changedByUserId, CancellationToken cancellationToken = default)
        {
            var statusHistory = new ShipmentStatusHistory
            {
                ShipmentId = shipment.Id,
                Status = status,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = changedByUserId
            };

            await _statusHistoryRepository.AddAsync(statusHistory, cancellationToken);
        }
    }
}
