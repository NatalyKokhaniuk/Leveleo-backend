using LeveLEO.Features.Identity.DTO;
using LeveLEO.Features.Identity.Models;
using Microsoft.AspNetCore.Identity.Data;
using System.Security.Claims;

namespace LeveLEO.Features.Identity.Services;

public interface IAuthService
{
    // ===== РЕЄСТРАЦІЯ ТА ПІДТВЕРДЖЕННЯ EMAIL =====
    Task RegisterAsync(RegisterRequestDto request, string backendBaseUrl);
    Task ConfirmEmailAsync(string userId, string token);
    Task ResendConfirmationEmailAsync(ResendEmailRequestDto request, string backendBaseUrl);

    // ===== ВХІД ТА ВИХІД =====
    Task<(AuthResponseDto? authResponse, string? RefreshToken)> LoginAndGetTokensAsync(LoginRequest request);
    Task<(AuthResponseDto? authResponse, string? RefreshToken)> VerifyTwoFactorAndGetTokensAsync(TwoFactorVerifyRequestDto request);
    Task LogoutAsync(string? refreshToken);

    // ===== РЕФРЕШ ТOKEN =====
    Task<AuthResponseDto> RefreshAccessTokenAsync(string refreshToken);

    // ===== 2FA =====
    Task<InitiateTwoFactorResponseDto> InitiateTwoFactorAsync(InitiateTwoFactorRequestDto request, ClaimsPrincipal user);
    Task<ConfirmTwoFactorSetupResponseDto> ConfirmTwoFactorSetupAsync(ConfirmTwoFactorSetupRequestDto request);

     Task<DisableTwoFactorResponseDto> DisableTwoFactorAsync(string userId);

    //// Отримання backup-кодів для TOTP
    Task<IEnumerable<string>> GetBackupCodesAsync(string userId);

    //// Логін за допомогою backup-коду
    Task<(AuthResponseDto authResponse, string RefreshToken)> LoginWithBackupCodeAsync(LoginWithBackupCodeRequestDto request);

    //// ===== ВІДНОВЛЕННЯ ПАРОЛЮ =====

    Task RequestPasswordResetAsync(RequestPasswordResetDto request, string frontendBaseUrl);
    Task ConfirmPasswordResetAsync(ConfirmPasswordResetDto request);
    Task ChangePasswordAsync(string userId, ChangePasswordRequestDto request);
    Task LogoutFromAllDevicesAsync(string userId);
    Task DeleteAccountAsync(string userId, string confirmEmail);
    Task<(AuthResponseDto authResponse, string refreshToken)> GenerateAuthResponseAsync(ApplicationUser user);
}
