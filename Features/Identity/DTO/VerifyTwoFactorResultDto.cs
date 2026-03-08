namespace LeveLEO.Features.Identity.DTO;

public class VerifyTwoFactorResultDto
{
    public string AccessToken { get; set; } = null!;
    public UserResponseDto User { get; set; } = null!;
}
