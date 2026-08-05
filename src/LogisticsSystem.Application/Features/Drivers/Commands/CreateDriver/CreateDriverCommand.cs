using MediatR;

namespace LogisticsSystem.Application.Features.Drivers.Commands.CreateDriver
{
    public sealed record CreateDriverCommand(string FirstName, string LastName, string Username, string Email, string Password, string LicenseNumber) : IRequest<Guid>;
}
