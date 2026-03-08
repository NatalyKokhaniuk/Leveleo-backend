using LeveLEO.Features.ProductAttributes.Models;

namespace LeveLEO.Features.ProductAttributes.DTO;

public class ProductAttributeResponseDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public AttributeType Type { get; set; }

    public string? Unit { get; set; }

    public bool IsFilterable { get; set; }

    public bool IsComparable { get; set; }

    public List<ProductAttributeTranslationResponseDto> Translations { get; set; } = [];
}