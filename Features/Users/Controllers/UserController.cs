using LeveLEO.Features.Users.DTO;
using LeveLEO.Features.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LeveLEO.Features.Users.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> GetUser(string id)
    {
        var user = await userService.GetUserByIdAsync(id);
        return Ok(user);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> EditUser(string id, [FromBody] UpdateUserDto request)
    {
        var user = await userService.EditUserAsync(id, request, isAdmin: User.IsInRole("Admin"));
        return Ok(user);
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new ApiException("UNAUTHORIZED", "Unauthorized", 401);

        var updatedUser = await userService.UpdateMyProfileAsync(userId, request);
        return Ok(updatedUser);
    }

    [HttpDelete("me")]
    [Authorize]
    public async Task<IActionResult> DeleteMyAccount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new ApiException("UNAUTHORIZED", "Unauthorized", 401);

        await userService.DeleteMyAccountAsync(userId);

        return Ok();
    }

    [HttpPost("{id}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChangeRoles(string id, [FromBody] string[] roles)
    {
        await userService.ChangeUserRolesAsync(id, roles);
        return Ok();
    }

    [HttpPost("{id}/block")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> SetActiveStatus(string id, [FromBody] bool isActive)
    {
        await userService.SetUserActiveStatusAsync(id, isActive);
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        await userService.DeleteUserAsync(id);
        return Ok();
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpPost("search")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SearchUsers([FromBody] UserFilterDto request)
    {
        var users = await userService.SearchUsersAsync(request);
        return Ok(users);
    }
}