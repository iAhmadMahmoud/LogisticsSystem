using LogisticsSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogisticsSystem.Application.Features.Drivers.Queries.GetAllDrivers
{
    public sealed class DriverListItemResponse
    {
        public Guid Id { get; init; }
        public Guid UseriD { get; init; }
        public string LicenseNumber { get; init; } = string.Empty;
        public DriverStatus Status { get; init; }
        public double? Latitude { get; init; }
        public double? Longtude { get; init; }
        public Guid? VechicleId { get; init; }
    }
}
