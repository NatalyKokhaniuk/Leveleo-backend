using LeveLEO.Features.Users.DTO;
using System.Threading.Tasks;
using LeveLEO.Features.Identity.DTO;

namespace LeveLEO.Features.Users.Services;

public interface IUserService
{
    // Отримати користувача по Id
    Task<UserResponseDto> GetUserByIdAsync(string userId);

    // Гнучке редагування користувача (тільки передані поля)
    Task<UserResponseDto> EditUserAsync(string userId, UpdateUserDto request, bool isAdmin = false);

    // Зміна ролей (тільки адміністратор)
    Task ChangeUserRolesAsync(string userId, string[] roles);

    // Блокування/розблокування користувача
    Task SetUserActiveStatusAsync(string userId, bool isActive);

    // "Soft delete" користувача
    Task DeleteUserAsync(string userId);
    Task DeleteMyAccountAsync(string userId);
    // Список всіх користувачів (з пагінацією, фільтрацією)
    Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
    Task <UserResponseDto> UpdateMyProfileAsync(string userId, UpdateMyProfileDto request);
    Task<IEnumerable<UserResponseDto>> SearchUsersAsync(UserFilterDto request);
}
