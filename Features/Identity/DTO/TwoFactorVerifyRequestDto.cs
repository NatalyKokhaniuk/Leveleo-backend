namespace LeveLEO.Features.Identity.DTO;

public class TwoFactorVerifyRequestDto
{
    public string TwoFaToken { get; set; } = null!;
    public string Code { get; set; } = null!;
}
