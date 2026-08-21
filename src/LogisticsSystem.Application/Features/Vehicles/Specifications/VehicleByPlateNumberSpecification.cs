using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.Vehicles.Specifications
{
    public class VehicleByPlateNumberSpecification : BaseSpecification<Vehicle>
    {
        public VehicleByPlateNumberSpecification(string plateNumber)
        {
            AddCriteria(v => v.PlateNumber.ToLower() == plateNumber.ToLower());
        }
    }
}
