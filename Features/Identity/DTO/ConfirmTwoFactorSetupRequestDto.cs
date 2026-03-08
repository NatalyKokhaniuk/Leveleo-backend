namespace LeveLEO.Features.Identity.DTO;

public class ConfirmTwoFactorSetupRequestDto
{
    public string Code { get; set; } = null!; // код від Email/SMS або TOTP код
    public string TemporaryToken { get; set; } = null!; // тимчасовий токен, отриманий при ініціації
}
