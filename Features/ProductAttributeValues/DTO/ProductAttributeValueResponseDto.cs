namespace LeveLEO.Features.ProductAttributeValues.DTO;

public class ProductAttributeValueResponseDto
{
    public Guid Id { get; set; }
    public Guid ProductAttributeId { get; set; }
    public string? StringValue { get; set; }
    public decimal? DecimalValue { get; set; }
    public int? IntValue { get; set; }
    public bool? BoolValue { get; set; }

    public List<ProductAttributeValueTranslationResponseDto> Translations { get; set; } = [];
}