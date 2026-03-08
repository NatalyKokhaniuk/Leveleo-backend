using LeveLEO.Features.Identity.Models;

namespace LeveLEO.Features.Identity.Services;

public interface ISocialAuthService
{
    Task<ApplicationUser> LoginWithGoogleAsync(string idToken);
    Task<ApplicationUser> LoginWithFacebookAsync(string accessToken);
    string GenerateTemporaryToken(ApplicationUser user);
    string GetFrontendRedirectUrl(string tempToken);
}
