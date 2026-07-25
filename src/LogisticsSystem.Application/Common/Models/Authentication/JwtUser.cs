namespace LogisticsSystem.Application.Common.Models.Authentication
{
    public class JwtUser
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public IReadOnlyList<string> Roles { get; set; } = new List<string>();
    }
}