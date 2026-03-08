using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.Identity.DTO;

public class RequestPasswordResetDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
