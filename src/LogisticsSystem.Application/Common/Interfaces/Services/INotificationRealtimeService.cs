using System;
using System.Collections.Generic;
using System.Text;

namespace LogisticsSystem.Application.Common.Interfaces.Services
{
    public interface INotificationRealtimeService
    {
        Task SendAsync(
            Guid userId,
            string title,
            string message,
            CancellationToken cancellationToken = default);
            
    }
}
