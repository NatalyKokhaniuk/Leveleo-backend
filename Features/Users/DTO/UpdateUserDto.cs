using LeveLEO.Infrastructure.Common;

namespace LeveLEO.Features.Users.DTO;

public class UpdateUserDto
{
    public Optional<string?> FirstName { get; set; } = default;
    public Optional<string?> LastName { get; set; } = default;
    public Optional<string?> AvatarKey { get; set; } = default;
    public Optional<string?> PhoneNumber { get; set; } = default; // тільки підтверджений
    public Optional<string?> Language { get; set; } = default;
    public Optional<bool> IsActive { get; set; } = default; // для admin
    public Optional<List<string>> Roles { get; set; } = default;// зміна ролей
}