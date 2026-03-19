using LeveLEO.Features.Identity.Enums;
using LeveLEO.Features.Identity.Models;
using LeveLEO.Settings;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace LeveLEO.Features.Identity.Services;

public class JwtService(JwtSettings jwtSettings) : IJwtService
{
    private readonly JwtSettings _jwtSettings = jwtSettings ?? throw new ArgumentNullException(nameof(jwtSettings));

    public string GenerateAccessToken(ApplicationUser user)
    {
        var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.Id), // 🔴
    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
    new Claim(JwtRegisteredClaimNames.Email, user.Email!),
    new Claim("firstName", user.FirstName ?? ""),
    new Claim("lastName", user.LastName ?? ""),
    new Claim("language", user.Language ?? "uk")
};

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string GenerateTwoFactorToken(string userId, string? code = null, TwoFactorMethod method = TwoFactorMethod.None, int expiresMinutes = 5)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim> { new Claim("userId", userId) };
        if (!string.IsNullOrEmpty(code))
            claims.Add(new Claim("2faCode", code));
        if (method != TwoFactorMethod.None)
            claims.Add(new Claim("2faMethod", method.ToString()));
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string HashRefreshToken(string refreshToken)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(refreshToken);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public string ValidateTwoFactorToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

        var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        }, out _);

        var userId = principal.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userId))
            throw new ApiException("INVALID_2FA_TOKEN", "Two-factor token is invalid", 400);

        return userId;
    }

    public (string userId, string? code, string? method) GetTwoFactorTokenClaims(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

        var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        }, out _);

        var userId = principal.FindFirst("userId")?.Value;
        var code = principal.FindFirst("2faCode")?.Value; // може бути null для TOTP
        var method = principal.FindFirst("2faMethod")?.Value; // може бути null

        if (string.IsNullOrEmpty(userId))
            throw new ApiException("INVALID_2FA_TOKEN", "Two-factor token is invalid", 400);

        return (userId, code, method);
    }

    //public string ValidateRefreshToken(string refreshToken)
    //{
    //    try
    //    {
    //        var tokenHandler = new JwtSecurityTokenHandler();
    //        var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);
    //        var principal = tokenHandler.ValidateToken(refreshToken, new TokenValidationParameters
    //        {
    //            ValidateIssuer = true,
    //            ValidIssuer = _jwtSettings.Issuer,
    //            ValidateAudience = true,
    //            ValidAudience = _jwtSettings.Audience,
    //            ValidateLifetime = true,
    //            IssuerSigningKey = new SymmetricSecurityKey(key),
    //            ValidateIssuerSigningKey = true,
    //            ClockSkew = TimeSpan.Zero
    //        }, out var validatedToken);

    //        var userId = principal.FindFirst("userId")?.Value;
    //        if (string.IsNullOrEmpty(userId))
    //            throw new ApiException("INVALID_REFRESH_TOKEN", "Refresh token is invalid", 401);

    //        return userId;
    //    }
    //    catch (SecurityTokenExpiredException)
    //    {
    //        throw new ApiException("REFRESH_TOKEN_EXPIRED", "Refresh token has expired", 401);
    //    }
    //    catch
    //    {
    //        throw new ApiException("INVALID_REFRESH_TOKEN", "Refresh token is invalid", 401);
    //    }
    //}

    public string GenerateTemporaryToken(ApplicationUser user, int expiresMinutes = 2)
    {
        var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.Id),
    new Claim("temp", "true")
};

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string ValidateTemporaryToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

        var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        }, out _);

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isTemp = principal.FindFirst("temp")?.Value;

        if (string.IsNullOrEmpty(userId) || isTemp != "true")
            throw new ApiException("INVALID_TEMP_TOKEN", "Temporary token is invalid", 401);

        return userId;
    }
}