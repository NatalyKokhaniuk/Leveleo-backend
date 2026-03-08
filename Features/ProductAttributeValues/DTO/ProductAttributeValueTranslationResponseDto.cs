namespace LeveLEO.Features.ProductAttributeValues.DTO;

public class ProductAttributeValueTranslationResponseDto
{
    public Guid Id { get; set; }
    public string LanguageCode { get; set; } = null!;
    public string Value { get; set; } = null!;
}