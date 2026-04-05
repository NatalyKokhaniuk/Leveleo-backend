using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Google.Apis.Auth;
using LeveLEO.Features.Identity.Models;

namespace LeveLEO.Features.Identity.Services;

public class SocialAuthService(
    UserManager<ApplicationUser> userManager,
    IJwtService jwtService,
    IConfiguration config,
    HttpClient httpClient) : ISocialAuthService
{
    /*public async Task<ApplicationUser> LoginWithGoogleAsync(string idToken)
    {
        var email = await ValidateGoogleTokenAsync(idToken); // email з Google
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser { Email = email, UserName = email, EmailConfirmed = true };
            await userManager.CreateAsync(user);
        }
        return user;
    }

    public async Task<ApplicationUser> LoginWithFacebookAsync(string accessToken)
    {
        var email = await ValidateFacebookTokenAsync(accessToken);
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser { Email = email, UserName = email, EmailConfirmed = true };
            await userManager.CreateAsync(user);
        }
        return user;
    }*/

    public async Task<ApplicationUser> LoginWithGoogleAsync(string idToken)
    {
        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);

        if (string.IsNullOrEmpty(payload.Email))
            throw new ApiException("GOOGLE_TOKEN_INVALID", "Email not found", 400);

        var user = await userManager.FindByEmailAsync(payload.Email);

        if (user == null)
        {
            user = new ApplicationUser
            {
                Email = payload.Email,
                UserName = payload.Email,
                EmailConfirmed = true,
                FirstName = payload.GivenName,
                LastName = payload.FamilyName
            };

            await userManager.CreateAsync(user);
        }

        return user;
    }

    public async Task<ApplicationUser> LoginWithFacebookAsync(string accessToken)
    {
        var response = await httpClient.GetAsync(
            $"https://graph.facebook.com/me?fields=email,first_name,last_name&access_token={accessToken}");

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var obj = JsonSerializer.Deserialize<JsonElement>(json);

        var email = obj.GetProperty("email").GetString();

        if (string.IsNullOrEmpty(email))
            throw new ApiException("FACEBOOK_TOKEN_INVALID", "Email not found", 400);

        var firstName = obj.TryGetProperty("first_name", out var fn) ? fn.GetString() : null;
        var lastName = obj.TryGetProperty("last_name", out var ln) ? ln.GetString() : null;

        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new ApplicationUser
            {
                Email = email,
                UserName = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName
            };

            await userManager.CreateAsync(user);
        }

        return user;
    }

    public string GenerateTemporaryToken(ApplicationUser user)
    {
        return jwtService.GenerateTemporaryToken(user, expiresMinutes: 2);
    }

    public string GetFrontendRedirectUrl(string tempToken)
    {
        // URL для редіректу на фронтенд
        var frontendUrl = config["Frontend:Url"]
                          ?? throw new ApiException("MISSING_FRONTEND_URL", "Frontend URL not configured", 500);

        // токен як query параметр
        return $"{frontendUrl}/social-login#token={tempToken}";
    }

    // --- приватні методи для перевірки токенів соцмереж ---
    private static async Task<string> ValidateGoogleTokenAsync(string idToken)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
            if (string.IsNullOrEmpty(payload.Email))
                throw new ApiException("GOOGLE_TOKEN_INVALID", "Email not found in Google token", 400);

            return payload.Email;
        }
        catch (Exception ex)
        {
            throw new ApiException("GOOGLE_TOKEN_INVALID", $"Invalid Google token: {ex.Message}", 400);
        }
    }

    private async Task<string> ValidateFacebookTokenAsync(string accessToken)
    {
        try
        {
            var fbResponse = await httpClient.GetAsync(
                $"https://graph.facebook.com/me?fields=email&access_token={accessToken}");

            fbResponse.EnsureSuccessStatusCode();

            var json = await fbResponse.Content.ReadAsStringAsync();
            var obj = JsonSerializer.Deserialize<JsonElement>(json);

            if (!obj.TryGetProperty("email", out var emailProperty))
                throw new ApiException("FACEBOOK_TOKEN_INVALID", "Email not found in Facebook token", 400);

            return emailProperty.GetString()!;
        }
        catch (Exception ex)
        {
            throw new ApiException("FACEBOOK_TOKEN_INVALID", $"Invalid Facebook token: {ex.Message}", 400);
        }
    }
}