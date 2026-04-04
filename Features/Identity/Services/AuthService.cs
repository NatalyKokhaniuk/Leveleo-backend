using LeveLEO.Data;
using LeveLEO.Features.Identity;
using LeveLEO.Features.Identity.DTO;
using LeveLEO.Features.Identity.Enums;
using LeveLEO.Features.Identity.Models;
using LeveLEO.Helpers;
using LeveLEO.Infrastructure.Email;
using LeveLEO.Infrastructure.Media.Services;
using LeveLEO.Infrastructure.SMS;
using LeveLEO.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;

namespace LeveLEO.Features.Identity.Services;

public class AuthService(
    IMediaService mediaService,
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    IJwtService jwtService,
    IEmailTemplateService emailTemplateService,
    ISmsSender smsSender,
    AppDbContext dbContext) : IAuthService
{
    // ================== REGISTER / EMAIL ==================
    public async Task RegisterAsync(RegisterRequestDto request, string backendBaseUrl)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Language = request.Language ?? "uk",
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new ApiException("VALIDATION_ERROR", "User registration failed", 400, IdentityErrorMapper.Map(result.Errors));

        var roleResult = await userManager.AddToRoleAsync(user, "User");
        if (!roleResult.Succeeded)
            throw new ApiException("ROLE_ASSIGNMENT_FAILED", "Failed to assign default role", 500, IdentityErrorMapper.Map(roleResult.Errors));

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = $"{backendBaseUrl.TrimEnd('/')}/api/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";
        var htmlMessage = emailTemplateService.GetRegistrationConfirmationEmail(confirmationLink);
        await emailSender.SendEmailAsync(request.Email, "Підтвердження реєстрації в LeveLEO", htmlMessage);
    }

    public async Task ConfirmEmailAsync(string userId, string token)
    {
        var user = await userManager.FindByIdAsync(userId) ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
            throw new ApiException("EMAIL_CONFIRMATION_FAILED", "Email confirmation failed", 400, IdentityErrorMapper.Map(result.Errors));
    }

    public async Task ResendConfirmationEmailAsync(ResendEmailRequestDto request, string backendBaseUrl)
    {
        if (string.IsNullOrEmpty(request.Email))
            throw new ApiException("EMAIL_REQUIRED", "Email is required", 400);

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null || user.EmailConfirmed)
            throw new ApiException("EMAIL_CONFIRMATION_PENDING", "If email exists and is not confirmed, a confirmation mail will be sent.", 400);

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = $"{backendBaseUrl.TrimEnd('/')}/api/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";
        var htmlMessage = emailTemplateService.GetRegistrationConfirmationEmail(confirmationLink);
        await emailSender.SendEmailAsync(request.Email, "Повторне підтвердження реєстрації в LeveLEO", htmlMessage);
    }

    // ================== LOGIN / LOGOUT ==================
    public async Task<(AuthResponseDto? authResponse, string? RefreshToken)> LoginAndGetTokensAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email)
            ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);

        if (!user.IsActive)
            throw new ApiException("USER_INACTIVE", "User is inactive", 403);
        if (!await userManager.IsEmailConfirmedAsync(user))
            throw new ApiException("EMAIL_NOT_CONFIRMED", "Email is not confirmed", 403);
        if (!await userManager.CheckPasswordAsync(user, request.Password))
            throw new ApiException("INVALID_CREDENTIALS", "Invalid email or password", 400);

        if (user.TwoFactorEnabled)
        {
            var method = user.TwoFactorMethod == TwoFactorMethod.None ? TwoFactorMethod.Email : user.TwoFactorMethod;

            if (method == TwoFactorMethod.Totp)
            {
                var twoFaToken = jwtService.GenerateTwoFactorToken(user.Id, null, TwoFactorMethod.Totp);
                return (new AuthResponseDto { Status = "2FA_REQUIRED", Method = "TOTP", TwoFaToken = twoFaToken }, null);
            }

            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString("D6");
            if (method == TwoFactorMethod.Email)
                await emailSender.SendEmailAsync(user.Email!, "Код підтвердження входу", emailTemplateService.TwoFactorCode(code));
            else if (method == TwoFactorMethod.Sms)
                await smsSender.SendSmsAsync(user.PhoneNumber!, $"Ваш код: {code}");

            var twoFaTokenWithCode = jwtService.GenerateTwoFactorToken(user.Id, code, method);
            return (new AuthResponseDto { Status = "2FA_REQUIRED", Method = method.ToString(), TwoFaToken = twoFaTokenWithCode }, null);
        }

        var userRoles = await userManager.GetRolesAsync(user);
        var accessToken = jwtService.GenerateAccessToken(user, userRoles);
        var refreshToken = jwtService.GenerateRefreshToken();
        var authUserDto = await BuildAuthUserDto(user);

        Console.WriteLine("=== LOGIN DEBUG ===");
        var refreshTokenEntity = new RefreshToken
        {
            TokenHash = jwtService.HashRefreshToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id
        };

        dbContext.RefreshTokens.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync();
        Console.WriteLine("Saved hash: " + jwtService.HashRefreshToken(refreshToken));

        return (new AuthResponseDto { User = authUserDto, AccessToken = accessToken }, refreshToken);
    }

    public async Task LogoutAsync(string? refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return;
        var tokenHash = jwtService.HashRefreshToken(refreshToken);
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        if (token != null)
        {
            dbContext.RefreshTokens.Remove(token);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<(AuthResponseDto? authResponse, string? RefreshToken)> VerifyTwoFactorAndGetTokensAsync(TwoFactorVerifyRequestDto request)
    {
        // Читаємо claims з токена (userId, code, method) — як в ConfirmTwoFactorSetupAsync
        var claims = jwtService.GetTwoFactorTokenClaims(request.TwoFaToken);
        var userId = claims.userId ?? throw new ApiException("INVALID_2FA_TOKEN", "Invalid temporary token", 400);

        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);

        var method = claims.method?.ToUpperInvariant() switch
        {
            "EMAIL" => TwoFactorMethod.Email,
            "SMS" => TwoFactorMethod.Sms,
            "TOTP" => TwoFactorMethod.Totp,
            _ => throw new ApiException("INVALID_2FA_METHOD", "Invalid 2FA method", 400)
        };

        if (method == TwoFactorMethod.Email || method == TwoFactorMethod.Sms)
        {
            if (claims.code != request.Code)
                throw new ApiException("INVALID_2FA_CODE", "The provided 2FA code is invalid", 400);
        }
        else if (method == TwoFactorMethod.Totp)
        {
            if (string.IsNullOrEmpty(user.TotpSecret))
                throw new ApiException("TOTP_NOT_INITIALIZED", "TOTP not initialized", 400);
            if (!VerifyTotp(user.TotpSecret, request.Code))
                throw new ApiException("INVALID_TOTP_CODE", "Invalid TOTP code", 400);
        }

        var userRoles = await userManager.GetRolesAsync(user);
        var accessToken = jwtService.GenerateAccessToken(user, userRoles);
        var refreshToken = jwtService.GenerateRefreshToken();
        var authUser = await BuildAuthUserDto(user);

        var refreshTokenEntity = new RefreshToken
        {
            TokenHash = jwtService.HashRefreshToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id
        };
        dbContext.RefreshTokens.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync();

        return (new AuthResponseDto { User = authUser, AccessToken = accessToken }, refreshToken);
    }

    public async Task<(AuthResponseDto? authResponse, string? RefreshToken, DateTimeOffset expiresAt)> RefreshAccessTokenAsync(string refreshToken)
    {
        var tokenHash = jwtService.HashRefreshToken(refreshToken);
        Console.WriteLine("=== REFRESH DEBUG ===");
        Console.WriteLine("Token: " + refreshToken);
        Console.WriteLine("Hash: " + tokenHash);

        var storedToken = await dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash)
            ?? throw new ApiException("INVALID_REFRESH_TOKEN", "Refresh token is invalid or expired", 401);

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
            throw new ApiException("INVALID_REFRESH_TOKEN", "Refresh token is invalid or expired", 401);

        if (storedToken.CreatedAt.Add(TimeSpan.FromDays(30)) <= DateTime.UtcNow)
            throw new ApiException("INVALID_REFRESH_TOKEN", "Refresh token is invalid or expired", 401);

        var user = await userManager.FindByIdAsync(storedToken.UserId)
            ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);

        // cleanup expired tokens
        var expiredTokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == user.Id && t.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();
        dbContext.RefreshTokens.RemoveRange(expiredTokens);

        // rotation
        dbContext.RefreshTokens.Remove(storedToken);

        var newRefreshToken = jwtService.GenerateRefreshToken();
        var newRefreshTokenEntity = new RefreshToken
        {
            TokenHash = jwtService.HashRefreshToken(newRefreshToken),
            ExpiresAt = storedToken.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id
        };

        dbContext.RefreshTokens.Add(newRefreshTokenEntity);
        await dbContext.SaveChangesAsync();

        var userRoles = await userManager.GetRolesAsync(user);
        var accessToken = jwtService.GenerateAccessToken(user, userRoles);
        var authUser = await BuildAuthUserDto(user);

        return (new AuthResponseDto { User = authUser, AccessToken = accessToken }, newRefreshToken, storedToken.ExpiresAt);
    }

    // ================== 2FA ==================
    public async Task<InitiateTwoFactorResponseDto> InitiateTwoFactorAsync(InitiateTwoFactorRequestDto request, ClaimsPrincipal user)
    {
        var userId = GetUserIdFromClaims(user);
        var appUser = await userManager.FindByIdAsync(userId) ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);

        string? totpSecret = null;
        string? code = null;

        switch (request.Method)
        {
            case TwoFactorMethod.Email:
                code = RandomNumberGenerator.GetInt32(100000, 999999).ToString("D6");
                await emailSender.SendEmailAsync(appUser.Email!, "Ваш код 2FA", $"Код: {code}");
                break;

            case TwoFactorMethod.Sms:
                if (string.IsNullOrEmpty(appUser.PhoneNumber))
                    throw new ApiException("PHONE_NOT_SET", "Phone number not set", 400);
                code = RandomNumberGenerator.GetInt32(100000, 999999).ToString("D6");
                await smsSender.SendSmsAsync(appUser.PhoneNumber!, $"Код: {code}");
                break;

            case TwoFactorMethod.Totp:
                totpSecret = GenerateTotpSecret();
                appUser.TotpSecret = totpSecret;
                await userManager.UpdateAsync(appUser);
                break;

            default:
                throw new ApiException("INVALID_2FA_METHOD", "Invalid 2FA method", 400);
        }

        var tempToken = jwtService.GenerateTwoFactorToken(userId, code, request.Method);
        return new InitiateTwoFactorResponseDto { Method = request.Method, TemporaryToken = tempToken, TotpSecret = totpSecret };
    }

    public async Task<ConfirmTwoFactorSetupResponseDto> ConfirmTwoFactorSetupAsync(ConfirmTwoFactorSetupRequestDto request)
    {
        var claims = jwtService.GetTwoFactorTokenClaims(request.TemporaryToken);
        var userId = claims.userId ?? throw new ApiException("INVALID_2FA_TOKEN", "Invalid temporary token", 400);

        var appUser = await userManager.FindByIdAsync(userId)
                      ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);

        var method = claims.method?.ToUpperInvariant() switch
        {
            "EMAIL" => TwoFactorMethod.Email,
            "SMS" => TwoFactorMethod.Sms,
            "TOTP" => TwoFactorMethod.Totp,
            _ => TwoFactorMethod.None
        };
        if (method == TwoFactorMethod.None)
            throw new ApiException("INVALID_2FA_METHOD", "Invalid 2FA method", 400);

        if ((method == TwoFactorMethod.Email || method == TwoFactorMethod.Sms) && claims.code != request.Code)
            throw new ApiException("INVALID_2FA_CODE", "Invalid 2FA code", 400);

        if (method == TwoFactorMethod.Totp)
        {
            if (string.IsNullOrEmpty(appUser.TotpSecret))
                throw new ApiException("TOTP_NOT_INITIALIZED", "TOTP not initialized for user", 400);

            if (!VerifyTotp(appUser.TotpSecret, request.Code))
                throw new ApiException("INVALID_TOTP_CODE", "Invalid TOTP code", 400);

            var codes = Enumerable.Range(0, 10)
                .Select(_ => RandomNumberGenerator.GetInt32(10000000, 99999999).ToString())
                .ToList();
            appUser.BackupCodes = string.Join(",", codes);
        }

        appUser.TwoFactorEnabled = true;
        appUser.TwoFactorMethod = method;
        await userManager.UpdateAsync(appUser);

        return new ConfirmTwoFactorSetupResponseDto
        {
            Success = true,
            Message = "2FA setup successfully",
            Method = method.ToString()
        };
    }

    // ================== HELPERS ==================
    private async Task<UserResponseDto> BuildAuthUserDto(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        string? avatarUrl = null;
        if (!string.IsNullOrEmpty(user.AvatarKey))
        {
            avatarUrl = await mediaService.GetFileUrlAsync(user.AvatarKey, TimeSpan.FromMinutes(30));
        }
        return new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName!,
            LastName = user.LastName!,
            Language = user.Language,
            AvatarUrl = avatarUrl,
            PhoneNumber = user.PhoneNumber,
            Roles = [.. roles],
            TwoFactorEnabled = user.TwoFactorEnabled,
            TwoFactorMethod = user.TwoFactorMethod,
            IsActive = user.IsActive
        };
    }

    private static string GetUserIdFromClaims(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new ApiException("USER_NOT_FOUND", "Cannot get user from token", 401);

    private static string GenerateTotpSecret() => Base32Helper.ToBase32(RandomNumberGenerator.GetBytes(20));

    private static bool VerifyTotp(string base32Secret, string code, int digits = 6, int step = 30, int allowedDrift = 1)
    {
        var key = Base32Helper.FromBase32(base32Secret);
        var timestep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / step;

        for (long i = -allowedDrift; i <= allowedDrift; i++)
        {
            if (GenerateTotpCode(key, timestep + i, digits) == code)
                return true;
        }
        return false;
    }

    private static string GenerateTotpCode(byte[] key, long timestep, int digits)
    {
        var data = BitConverter.GetBytes(timestep);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(data);

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(data);

        int offset = hash[^1] & 0x0F;
        int binary = ((hash[offset] & 0x7F) << 24) |
                     ((hash[offset + 1] & 0xFF) << 16) |
                     ((hash[offset + 2] & 0xFF) << 8) |
                     (hash[offset + 3] & 0xFF);

        int totp = binary % (int)Math.Pow(10, digits);
        return totp.ToString(new string('0', digits));
    }

    public async Task<DisableTwoFactorResponseDto> DisableTwoFactorAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId) ?? throw new ApiException("USER_NOT_FOUND", "User not found", 400);
        user.TwoFactorMethod = TwoFactorMethod.None;
        user.TotpSecret = null;
        user.BackupCodes = null;
        user.TwoFactorEnabled = false;
        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
            throw new ApiException("DISABLE_2FA_FAILED", "Disabling 2FA failed", 400);

        return new DisableTwoFactorResponseDto
        {
            Success = true,
            Message = "Two-factor authentication has been disabled."
        };
    }

    public async Task<IEnumerable<string>> GetBackupCodesAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId) ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);
        var existingCodes = user.BackupCodes?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? [];
        if (existingCodes.Count >= 10)
            return existingCodes;
        var newCodes = Enumerable.Range(0, 10 - existingCodes.Count)
            .Select(_ => RandomNumberGenerator.GetInt32(10000000, 99999999).ToString())
            .ToList();
        existingCodes.AddRange(newCodes);
        user.BackupCodes = string.Join(",", existingCodes);
        await userManager.UpdateAsync(user);
        return existingCodes;
    }

    public async Task<(AuthResponseDto authResponse, string RefreshToken)> LoginWithBackupCodeAsync(LoginWithBackupCodeRequestDto request)
    {
        var user = await userManager.FindByEmailAsync(request.Email)
            ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);

        if (!user.IsActive)
            throw new ApiException("USER_INACTIVE", "User is inactive", 403);
        if (!await userManager.IsEmailConfirmedAsync(user))
            throw new ApiException("EMAIL_NOT_CONFIRMED", "Email is not confirmed", 403);
        if (!user.TwoFactorEnabled)
            throw new ApiException("2FA_NOT_ENABLED", "Two-factor authentication is not enabled for this user", 400);

        if (string.IsNullOrEmpty(user.BackupCodes))
            throw new ApiException("NO_BACKUP_CODES", "No backup codes available", 400);

        var codes = user.BackupCodes.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (!codes.Contains(request.BackupCode))
            throw new ApiException("INVALID_BACKUP_CODE", "Invalid backup code", 400);

        codes.Remove(request.BackupCode);
        user.BackupCodes = string.Join(',', codes);
        await userManager.UpdateAsync(user);

        var userRoles = await userManager.GetRolesAsync(user);
        var accessToken = jwtService.GenerateAccessToken(user, userRoles);
        var refreshToken = jwtService.GenerateRefreshToken();
        var authUser = await BuildAuthUserDto(user);

        var refreshTokenEntity = new RefreshToken
        {
            TokenHash = jwtService.HashRefreshToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id
        };
        dbContext.RefreshTokens.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync();

        return (new AuthResponseDto { User = authUser, AccessToken = accessToken }, refreshToken);
    }

    public async Task RequestPasswordResetAsync(RequestPasswordResetDto request, string frontendBaseUrl)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.EmailConfirmed)
            return;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = $"{frontendBaseUrl.TrimEnd('/')}/reset-password?userId={user.Id}&token={Uri.EscapeDataString(token)}";
        var emailHtml = emailTemplateService.GetPasswordResetEmail(resetLink);
        await emailSender.SendEmailAsync(user.Email!, "Скидання пароля", emailHtml);
    }

    public async Task ConfirmPasswordResetAsync(ConfirmPasswordResetDto request)
    {
        var user = await userManager.FindByIdAsync(request.UserId)
            ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            throw new ApiException("PASSWORD_RESET_FAILED", "Password reset failed", 400, IdentityErrorMapper.Map(result.Errors));

        await dbContext.RefreshTokens.Where(t => t.UserId == user.Id).ExecuteDeleteAsync();
        await userManager.UpdateSecurityStampAsync(user);
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequestDto request)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);
        if (!result.Succeeded)
            throw new ApiException("CHANGE_PASSWORD_FAILED", "Password change failed", 400, IdentityErrorMapper.Map(result.Errors));

        await dbContext.RefreshTokens.Where(t => t.UserId == user.Id).ExecuteDeleteAsync();
        await userManager.UpdateSecurityStampAsync(user);
    }

    public async Task LogoutFromAllDevicesAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);

        await dbContext.RefreshTokens.Where(t => t.UserId == user.Id).ExecuteDeleteAsync();
        await userManager.UpdateSecurityStampAsync(user);
    }

    public async Task DeleteAccountAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);

        user.Email = $"deleted_{Guid.NewGuid()}@example.com";
        user.UserName = user.Email;
        user.FirstName = null;
        user.LastName = null;
        user.PhoneNumber = null;
        user.AvatarKey = null;
        user.IsActive = false;
        user.TwoFactorEnabled = false;
        user.TwoFactorMethod = TwoFactorMethod.None;
        user.TotpSecret = null;
        user.BackupCodes = null;
        user.IsDeleted = true;

        await dbContext.RefreshTokens.Where(t => t.UserId == user.Id).ExecuteDeleteAsync();
        await userManager.UpdateSecurityStampAsync(user);
        await userManager.UpdateAsync(user);
    }

    public async Task<(AuthResponseDto authResponse, string refreshToken)> GenerateAuthResponseAsync(ApplicationUser user)
    {
        if (user == null)
            throw new ApiException("USER_NOT_FOUND", "User not found", 404);
        if (!user.IsActive)
            throw new ApiException("USER_INACTIVE", "User is inactive", 403);
        if (!await userManager.IsEmailConfirmedAsync(user))
            throw new ApiException("EMAIL_NOT_CONFIRMED", "Email is not confirmed", 403);

        var userRoles = await userManager.GetRolesAsync(user);
        var accessToken = jwtService.GenerateAccessToken(user, userRoles);
        var refreshToken = jwtService.GenerateRefreshToken();
        var authUserDto = await BuildAuthUserDto(user);

        var refreshTokenEntity = new RefreshToken
        {
            TokenHash = jwtService.HashRefreshToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id
        };
        dbContext.RefreshTokens.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync();

        return (new AuthResponseDto { User = authUserDto, AccessToken = accessToken }, refreshToken);
    }
}