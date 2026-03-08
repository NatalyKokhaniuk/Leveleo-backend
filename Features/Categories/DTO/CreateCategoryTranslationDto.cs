using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.Categories.DTO;

/// <summary>
/// DTO для перекладу категорії
/// </summary>
public class CreateCategoryTranslationDto
{
    [Required]
    [MaxLength(5)]
    public string LanguageCode { get; set; } = null!;

    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}