namespace LogisticsSystem.Application.Common.Models.Authentication
{
    public sealed class ResetPasswordRequest
    {
        public Guid UserId { get; set; }
        //public string Email { get; init; } = string.Empty;
        public string Token { get; init; } = string.Empty;
        public string NewPassword { get; init; } = string.Empty;
    }
}
