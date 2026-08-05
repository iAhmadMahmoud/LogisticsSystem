namespace LogisticsSystem.Api.Contracts.Drivers
{
    public sealed class CreateDriverRequest
    {
        public string FirstName { get; init; } = string.Empty; 
        public string LastName { get; init; } = string.Empty; 
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty; 
        public string Password { get; init; } = string.Empty;
        public string LicenseNumber { get; init; } = string.Empty;
    }
}
