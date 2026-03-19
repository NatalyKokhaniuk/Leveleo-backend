using LeveLEO.Features.Identity.DTO.SocialLogin;
using LeveLEO.Features.Identity.Models;
using LeveLEO.Features.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LeveLEO.Features.Identity.Controllers;

[Route("api/auth/social")]
[ApiController]
public class SocialAuthController(
    IWebHostEnvironment env,
    ISocialAuthService socialAuthService,
    IJwtService jwtService,
    UserManager<ApplicationUser> userManager,
    IAuthService authService) : ControllerBase
{
    // ================== GOOGLE LOGIN ==================
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithGoogle([FromBody] SocialLoginRequestDto request)
    {
        var user = await socialAuthService.LoginWithGoogleAsync(request.AccessToken);
        var tempToken = socialAuthService.GenerateTemporaryToken(user);
        var redirectUrl = socialAuthService.GetFrontendRedirectUrl(tempToken);

        return Ok(new { redirectUrl });
    }

    // ================== FACEBOOK LOGIN ==================
    [HttpPost("facebook")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithFacebook([FromBody] SocialLoginRequestDto request)
    {
        var user = await socialAuthService.LoginWithFacebookAsync(request.AccessToken);
        var tempToken = socialAuthService.GenerateTemporaryToken(user);
        var redirectUrl = socialAuthService.GetFrontendRedirectUrl(tempToken);

        return Ok(new { redirectUrl });
    }

    [HttpPost("exchange")]
    [AllowAnonymous]
    public async Task<IActionResult> ExchangeTemporaryToken([FromBody] ExchangeTempTokenRequestDto request)
    {
        var userId = jwtService.ValidateTemporaryToken(request.TempToken);

        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);

        var (authResponse, refreshToken) = await authService.GenerateAuthResponseAsync(user);

        if (!string.IsNullOrEmpty(refreshToken))
        {
            var isLocal = env.IsDevelopment();
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = !isLocal,
                SameSite = isLocal ? SameSiteMode.Lax : SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
        }

        return Ok(authResponse);
    }
}