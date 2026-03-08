namespace LeveLEO.Features.Identity.DTO.SocialLogin;

public class SocialLoginRequestDto
{
    public string Provider { get; set; } = default!; // "google" або "facebook"
    public string AccessToken { get; set; } = default!;
}
