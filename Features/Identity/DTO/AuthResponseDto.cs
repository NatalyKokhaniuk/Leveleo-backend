namespace LeveLEO.Features.Identity.DTO;

public class AuthResponseDto
{
    public UserResponseDto? User { get; set; } = null!;
    public string? AccessToken { get; set; }

    public string? Status = "2FA_REQUIRED";
    public string? Method { get; init; }
    public string? TwoFaToken { get; init; } = null!;
}
