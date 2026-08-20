namespace LogisticsSystem.Application.Common.Models.Authentication
{
    public sealed record UserInfoDto(
        Guid Id,
        string FullName,
        string? Email,
        string? PhoneNumber);
}
