using System.ComponentModel.DataAnnotations;
using LeveLEO.Features.ContactForms.Models;

namespace LeveLEO.Features.ContactForms.DTO;

public class CreateContactFormDto
{
    /// <summary>
    /// Тема звернення
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = null!;

    /// <summary>
    /// Текст звернення
    /// </summary>
    [Required]
    [MinLength(10)]
    public string Message { get; set; } = null!;

    /// <summary>
    /// Категорія звернення
    /// </summary>
    [Required]
    public ContactFormCategory Category { get; set; }

    /// <summary>
    /// Email заявника
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    /// <summary>
    /// Телефон (необов'язково)
    /// </summary>
    [Phone]
    [MaxLength(30)]
    public string? Phone { get; set; }
}

public class ContactFormResponseDto
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = null!;
    public string Message { get; set; } = null!;
    public ContactFormCategory Category { get; set; }
    public string CategoryDisplay { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
