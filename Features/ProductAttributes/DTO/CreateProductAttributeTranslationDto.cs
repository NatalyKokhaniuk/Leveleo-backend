namespace LeveLEO.Features.ProductAttributes.DTO;

public class CreateProductAttributeTranslationDto
{
    public string LanguageCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}