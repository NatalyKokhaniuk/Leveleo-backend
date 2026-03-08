using LeveLEO.Features.Identity.DTO;

namespace LeveLEO.Features.Identity.DTO.SocialLogin;

public class SocialLoginResponseDto
{
    public AuthResponseDto Auth { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
}
