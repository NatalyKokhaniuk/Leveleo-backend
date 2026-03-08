using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.Identity.DTO;

public class ChangePasswordRequestDto
{
    [Required]
    public string NewPassword { get; set; } = string.Empty;
}
