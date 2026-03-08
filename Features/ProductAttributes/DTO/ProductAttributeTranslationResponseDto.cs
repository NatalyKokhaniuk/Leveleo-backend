namespace LeveLEO.Features.ProductAttributes.DTO;

public class ProductAttributeTranslationResponseDto
{
    public Guid Id { get; set; }

    public string LanguageCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}