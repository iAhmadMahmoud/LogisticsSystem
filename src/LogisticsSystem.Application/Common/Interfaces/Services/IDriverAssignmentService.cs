using LogisticsSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogisticsSystem.Application.Common.Interfaces.Services
{
    public interface IDriverAssignmentService
    {
        Task<Driver?> FindBestAvailableDriverAsync(double pickupLatitude, double pickupLongitude, CancellationToken cancellationToken = default);
    }
}
