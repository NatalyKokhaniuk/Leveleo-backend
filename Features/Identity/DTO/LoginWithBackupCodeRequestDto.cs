using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.Identity.DTO;

public class LoginWithBackupCodeRequestDto
{
    [Required]
    public string Email { get; init; } = null!;

    [Required]
    public string BackupCode { get; init; } = null!;
}
