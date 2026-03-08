using LeveLEO.Features.Identity.Enums;

namespace LeveLEO.Features.Identity.DTO;

public class UserResponseDto
{
    public string Id { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Language { get; set; } = "uk";
    public string? AvatarUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public string[] Roles { get; set; } = null!;

    // 2FA
    public bool TwoFactorEnabled { get; set; }
    public TwoFactorMethod? TwoFactorMethod { get; set; } // Email, SMS, TOTP, null
}
