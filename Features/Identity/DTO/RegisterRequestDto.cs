using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.Identity.DTO;

public class RegisterRequestDto

{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Language { get; set; }
}