namespace LogisticsSystem.Api.Contracts.Users
{
    public sealed record UpdateUserRequest(
        string FirstName,
        string LastName,
        string? PhoneNumber,
        string Email,
        string UserName);
}
