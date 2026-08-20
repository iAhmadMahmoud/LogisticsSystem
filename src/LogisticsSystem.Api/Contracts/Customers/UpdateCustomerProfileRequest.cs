namespace LogisticsSystem.Api.Contracts.Customers
{
    public sealed record UpdateCustomerProfileRequest(
        string FirstName,
        string LastName,
        string? PhoneNumber,
        string? DefaultAddress);
}
