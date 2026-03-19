using LeveLEO.Features.Identity.Enums;
using LeveLEO.Features.Identity.Models;

namespace LeveLEO.Features.Identity.Services;

public interface IJwtService
{
    string GenerateAccessToken(ApplicationUser user);

    string GenerateRefreshToken();

    string GenerateTwoFactorToken(string userId, string? code = null, TwoFactorMethod method = TwoFactorMethod.None, int expiresMinutes = 5);

    string ValidateTwoFactorToken(string token);

    //string ValidateRefreshToken(string refreshToken);

    (string userId, string? code, string? method) GetTwoFactorTokenClaims(string token);

    string HashRefreshToken(string refreshToken);

    string GenerateTemporaryToken(ApplicationUser user, int expiresMinutes = 2);

    string ValidateTemporaryToken(string token);
}