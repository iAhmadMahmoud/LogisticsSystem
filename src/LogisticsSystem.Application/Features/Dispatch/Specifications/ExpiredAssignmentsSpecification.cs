using LogisticsSystem.Application.Common.Specifications;
using LogisticsSystem.Domain.Entities;
using LogisticsSystem.Domain.Enums;

namespace LogisticsSystem.Application.Features.Dispatch.Specifications
{
    public sealed class ExpiredAssignmentsSpecification : BaseSpecification<DispatchAssignment>
    {
        public ExpiredAssignmentsSpecification(DateTime expirationTime) 
            : base (x=>
                x.Status == AssignmentStatus.Pending && 
                x.SentAt<= expirationTime)
        {
            AddInclude(x=>x.Shipment);
        }
    }
}
