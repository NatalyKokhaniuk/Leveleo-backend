using LeveLEO.Infrastructure.Common;

namespace LeveLEO.Features.Users.DTO;

public class UpdateMyProfileDto
{
    public Optional<string?> FirstName { get; set; } = default;
    public Optional<string?> LastName { get; set; } = default;
    public Optional<string?> AvatarKey { get; set; } = default;
    public Optional<string?> PendingPhoneNumber { get; set; } = default;
    public Optional<string?> Language { get; set; } = default;
}