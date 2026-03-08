using LeveLEO.Models;

namespace LeveLEO.Features.Identity.Models;

public class RefreshToken : ITimestamped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TokenHash { get; set; } = null!; // зберігаємо SHA256 від токена
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool Revoked { get; set; } = false;
}