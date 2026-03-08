using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.Brands.DTO;

public class CreateBrandTranslationDto
{
    [Required]
    [MaxLength(5)]
    public string LanguageCode { get; set; } = null!;

    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}