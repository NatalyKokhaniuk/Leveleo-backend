using LeveLEO.Features.Identity.Enums;

namespace LeveLEO.Features.Identity.DTO;

public class InitiateTwoFactorResponseDto
{
    public TwoFactorMethod Method { get; init; }
    public string TemporaryToken { get; init; } = null!;// JWT або GUID для підтвердження 2FA
    public string? TotpSecret { get; init; } // тільки для TOTP
}
