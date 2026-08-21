using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;

namespace LogisticsSystem.Application.Features.Vehicles.Specifications
{
    public class VehicleByIdWithDriverSpecification : BaseSpecification<Vehicle>
    {
        public VehicleByIdWithDriverSpecification(Guid id)
        {
            AddCriteria(v => v.Id == id);
            AddInclude(v => v.Driver!);
        }
    }
}
