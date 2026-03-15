using LeveLEO.Features.Identity.DTO;
using LeveLEO.Features.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Sprache;
using System.Security.Claims;

namespace LeveLEO.Features.Identity.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController(IWebHostEnvironment env, IAuthService authService, IConfiguration config) : ControllerBase
{
    // ================== REGISTER ==================
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        await authService.RegisterAsync(request, baseUrl);
        return Ok(new { message = "USER_CREATED_CHECK_EMAIL" });
    }

    // ================== EMAIL CONFIRMATION ==================
    [HttpGet("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        try
        {
            await authService.ConfirmEmailAsync(userId, token);
            return Redirect($"{config["Frontend:Url"]}/email-confirmed");
        }
        catch (ApiException ex)
        {
            return Redirect($"{config["Frontend:Url"]}/email-confirmation-error?code={ex.ErrorCode}");
        }
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendEmailRequestDto request)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        await authService.ResendConfirmationEmailAsync(request, baseUrl);
        return Ok(new { message = "CONFIRMATION_EMAIL_SENT" });
    }

    // ================== LOGIN / LOGOUT ==================
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        Console.WriteLine("Login endpoint hit");
        var internalRequest = new LoginRequest
        {
            Email = request.Email,
            Password = request.Password
        };
        var (authResponse, refreshToken) = await authService.LoginAndGetTokensAsync(internalRequest);

        if (authResponse == null)
            throw new ApiException("LOGIN_FAILED", "Authentication failed", 400);

        if (refreshToken != null)
        {
            var isLocal = env.IsDevelopment();
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = !isLocal,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
        }

        return Ok(authResponse);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        //var user = HttpContext.User;

        // Логіка логауту на бекенді (видалення з БД або Blacklist)
        await authService.LogoutAsync(refreshToken);
        if (Request.Cookies.ContainsKey("refreshToken"))
        {
            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });
        }

        return Ok(new { message = "LOGGED_OUT" });
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken()
    {
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
            throw new ApiException("REFRESH_TOKEN_MISSING", "Refresh token is missing", 401);

        var authResponse = await authService.RefreshAccessTokenAsync(refreshToken);
        return Ok(authResponse);
    }

    // ================== TWO FACTOR AUTH ==================
    [HttpPost("2fa/initiate")]
    [Authorize]
    public async Task<IActionResult> InitiateTwoFactor([FromBody] InitiateTwoFactorRequestDto request)
    {
        var user = HttpContext.User;
        var response = await authService.InitiateTwoFactorAsync(request, user);
        return Ok(response);
    }

    [HttpPost("2fa/confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmTwoFactor([FromBody] ConfirmTwoFactorSetupRequestDto request)
    {
        var response = await authService.ConfirmTwoFactorSetupAsync(request);
        return Ok(response);
    }

    [HttpPost("2fa/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] TwoFactorVerifyRequestDto request)
    {
        var (authResponse, refreshToken) = await authService.VerifyTwoFactorAndGetTokensAsync(request);

        if (refreshToken != null)
        {
            var isLocal = env.IsDevelopment();
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = !isLocal,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
        }

        return Ok(authResponse);
    }

    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor()
    {
        //  userId з токена
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new ApiException("UNAUTHORIZED", "Unauthorized", 401);

        var response = await authService.DisableTwoFactorAsync(userId);
        if (!response.Success)
            throw new ApiException("DISABLE_2FA_ERROR", "Disable 2fa failed", 400);

        return Ok(response);
    }

    [HttpGet("2fa/backup-codes")]
    [Authorize]
    public async Task<IActionResult> GetBackupCodes()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new ApiException("UNAUTHORIZED", "Unauthorized", 401);

        var codes = await authService.GetBackupCodesAsync(userId);

        return Ok(new { Codes = codes });
    }

    [HttpPost("2fa/backup-login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithBackupCode([FromBody] LoginWithBackupCodeRequestDto request)
    {
        var (authResponse, refreshToken) = await authService.LoginWithBackupCodeAsync(request);
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var isLocal = env.IsDevelopment();
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = !isLocal,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
        }
        return Ok(authResponse);
    }

    [HttpPost("password-reset/request")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestPasswordReset(
        [FromBody] RequestPasswordResetDto request)
    {
        var frontendUrl = config["Frontend:Url"] ?? throw new ApiException("MISSING_FRONTEND_URL_CONFIGURATION", "Frontend:Url configuration is missing", 400);
        await authService.RequestPasswordResetAsync(
            request,
            frontendUrl
        );

        return Ok();
    }

    [HttpPost("password-reset/confirm")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmPasswordReset(
    [FromBody] ConfirmPasswordResetDto request)
    {
        await authService.ConfirmPasswordResetAsync(request);

        return Ok();
    }

    [HttpPost("password/change")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
    [FromBody] ChangePasswordRequestDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new ApiException("UNAUTHORIZED", "Unauthorized", 401);

        await authService.ChangePasswordAsync(userId, request);
        return Ok();
    }

    [HttpPost("logout/all")]
    [Authorize]
    public async Task<IActionResult> LogoutFromAllDevices()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new ApiException("UNAUTHORIZED", "Unauthorized", 401);
        if (Request.Cookies.ContainsKey("refreshToken"))
        {
            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });
        }
        await authService.LogoutFromAllDevicesAsync(userId);
        return Ok();
    }

    [HttpDelete("delete-account")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new ApiException("UNAUTHORIZED", "Unauthorized", 401);

        await authService.DeleteAccountAsync(userId);

        if (Request.Cookies.ContainsKey("refreshToken"))
        {
            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });
        }

        return Ok(new { message = "ACCOUNT_DELETED" });
    }
}
