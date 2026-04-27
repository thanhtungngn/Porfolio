namespace Portfolio.Api.Infrastructure.Persistence.Entities;

public sealed class UserAccount
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<IngestedFileRecord> IngestedFiles { get; set; } = [];
}