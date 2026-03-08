using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.Brands.DTO;

public class CreateBrandDto
{
    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? LogoKey { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public List<CreateBrandTranslationDto>? Translations { get; set; }
}