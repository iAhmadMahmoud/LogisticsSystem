using MediatR;

namespace LogisticsSystem.Application.Features.Customers.Commands.UpdateCustomerProfile
{
    public sealed record UpdateCustomerProfileCommand(
        string FirstName,
        string LastName,
        string? PhoneNumber,
        string? DefaultAddress) : IRequest;
}
