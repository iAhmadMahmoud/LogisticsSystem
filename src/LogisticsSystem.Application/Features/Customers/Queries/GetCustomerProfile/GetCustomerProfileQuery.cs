using LogisticsSystem.Application.Features.Customers.DTOs;
using MediatR;

namespace LogisticsSystem.Application.Features.Customers.Queries.GetCustomerProfile
{
    public sealed record GetCustomerProfileQuery : IRequest<CustomerProfileDto>;
}
