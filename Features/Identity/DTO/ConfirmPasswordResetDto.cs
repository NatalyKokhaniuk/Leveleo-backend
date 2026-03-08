using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.Identity.DTO;

public class ConfirmPasswordResetDto
{
    [Required] public string UserId { get; set; } = string.Empty;
    [Required] public string Token { get; set; } = string.Empty;
    [Required] public string NewPassword { get; set; } = string.Empty;
}
