using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.Identity.DTO;

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = null!;

    [Required]
    public string Password { get; init; } = null!;
}
