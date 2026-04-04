using LeveLEO.Features.Users.DTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LeveLEO.Features.Identity.DTO;
using LeveLEO.Features.Identity.Models;
using LeveLEO.Infrastructure.Media.Services;

namespace LeveLEO.Features.Users.Services;

public class UserService(UserManager<ApplicationUser> userManager, IMediaService mediaService) : IUserService
{
    public async Task ChangeUserRolesAsync(string userId, string[] roles)
    {
        var user = await userManager.FindByIdAsync(userId)
                ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);
        var currentRoles = await userManager.GetRolesAsync(user);
        var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
            throw new ApiException("ROLE_REMOVE_FAILED", "Failed to remove roles", 500, removeResult.Errors);

        var addResult = await userManager.AddToRolesAsync(user, roles);
        if (!addResult.Succeeded)
            throw new ApiException("ROLE_ADD_FAILED", "Failed to add roles", 400);
    }

    public async Task DeleteUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);
        user.IsDeleted = true;
        user.IsActive = false;
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            throw new ApiException("USER_DELETE_FAILED", "Failed to delete user", 400);
    }

    public async Task<UserResponseDto> EditUserAsync(string userId, UpdateUserDto request, bool isAdmin = false)
    {
        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);

        if (request.FirstName.HasValue)
            user.FirstName = request.FirstName.Value;

        if (request.LastName.HasValue)
            user.LastName = request.LastName.Value;

        if (request.Language.HasValue && !string.IsNullOrEmpty(request.Language.Value))
            user.Language = request.Language.Value;

        if (request.AvatarKey.HasValue)
        {
            if (!string.IsNullOrEmpty(user.AvatarKey) && user.AvatarKey != request.AvatarKey.Value)
                await mediaService.DeleteFileAsync(user.AvatarKey);

            user.AvatarKey = request.AvatarKey.Value;
        }

        if (request.PhoneNumber.HasValue)
            user.PhoneNumber = request.PhoneNumber.Value;

        if (isAdmin && request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        if (isAdmin && request.Roles.HasValue && request.Roles.Value != null)
            await ChangeUserRolesAsync(user.Id, [.. request.Roles.Value]);

        await userManager.UpdateAsync(user);

        return await BuildUserResponseDto(user);
    }

    public async Task DeleteMyAccountAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);

        user.IsDeleted = true;
        user.IsActive = false;

        await userManager.UpdateAsync(user);
    }

    private async Task<UserResponseDto> BuildUserResponseDto(ApplicationUser user)
    {
        // Побудова UserResponseDto з ApplicationUser

        if (user is null)
        {
            throw new ApiException("USER_NOT_FOUND", "User not found", 404);
        }
        var roles = await userManager.GetRolesAsync(user);
        string? avatarUrl = null;
        if (!string.IsNullOrEmpty(user.AvatarKey))
        {
            // тимчасовий URL
            avatarUrl = await mediaService.GetFileUrlAsync(user.AvatarKey, TimeSpan.FromMinutes(30));
        }
        return new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            Language = user.Language,
            AvatarUrl = avatarUrl,
            PhoneNumber = user.PhoneNumber,
            Roles = [.. roles],
            TwoFactorEnabled = user.TwoFactorEnabled,
            TwoFactorMethod = user.TwoFactorMethod,
            IsActive = user.IsActive,
        };
    }

    public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
    {
        var users = await userManager.Users.ToListAsync();

        var result = new List<UserResponseDto>();
        foreach (var user in users)
        {
            result.Add(await BuildUserResponseDto(user));
        }

        return result;
    }

    public async Task<UserResponseDto> GetUserByIdAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);

        return await BuildUserResponseDto(user);
    }

    public async Task SetUserActiveStatusAsync(string userId, bool isActive)
    {
        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);

        user.IsActive = isActive;

        await userManager.UpdateAsync(user);
    }

    public async Task<UserResponseDto> UpdateMyProfileAsync(string userId, UpdateMyProfileDto request)
    {
        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new ApiException("USER_NOT_FOUND", "User not found", 404);

        if (request.FirstName.HasValue)
            user.FirstName = request.FirstName.Value;

        if (request.LastName.HasValue)
            user.LastName = request.LastName.Value;

        if (request.Language.HasValue && !string.IsNullOrEmpty(request.Language.Value))
            user.Language = request.Language.Value;
        if (request.AvatarKey.HasValue)
        {
            if (!string.IsNullOrEmpty(user.AvatarKey) && user.AvatarKey != request.AvatarKey.Value)
                await mediaService.DeleteFileAsync(user.AvatarKey);

            user.AvatarKey = request.AvatarKey.Value;
        }

        if (request.PendingPhoneNumber.HasValue)
        {
            user.PhoneNumber = request.PendingPhoneNumber.Value;
        }

        await userManager.UpdateAsync(user);

        return await BuildUserResponseDto(user);
    }

    public async Task<IEnumerable<UserResponseDto>> SearchUsersAsync(UserFilterDto request)
    {
        var query = userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Email))
            query = query.Where(u => u.Email != null && u.Email.Contains(request.Email));

        if (!string.IsNullOrWhiteSpace(request.FirstName))
            query = query.Where(u => u.FirstName != null && u.FirstName.Contains(request.FirstName));

        if (!string.IsNullOrWhiteSpace(request.LastName))
            query = query.Where(u => u.LastName != null && u.LastName.Contains(request.LastName));

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            query = query.Where(u => u.PhoneNumber != null && u.PhoneNumber.Contains(request.PhoneNumber));

        if (request.IsActive.HasValue)
            query = query.Where(u => u.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var roleFilteredUsers = new List<ApplicationUser>();
            var allUsers = await query.ToListAsync();

            foreach (var user in allUsers)
            {
                var roles = await userManager.GetRolesAsync(user);
                if (roles.Contains(request.Role))
                    roleFilteredUsers.Add(user);
            }

            query = roleFilteredUsers.AsQueryable();
        }

        var usersList = await query.ToListAsync();

        var result = new List<UserResponseDto>();
        foreach (var user in usersList)
        {
            result.Add(await BuildUserResponseDto(user));
        }

        return result;
    }
}