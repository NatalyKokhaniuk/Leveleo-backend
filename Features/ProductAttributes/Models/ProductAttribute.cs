using LeveLEO.Features.AttributeGroups.Models;
using LeveLEO.Features.ProductAttributeValues.Models;
using LeveLEO.Features.Products.Models;
using LeveLEO.Infrastructure.Translation.Models;
using LeveLEO.Models;

namespace LeveLEO.Features.ProductAttributes.Models;

public class ProductAttribute : ITimestamped, ITranslatable<ProductAttributeTranslation>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AttributeGroupId { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public AttributeType Type { get; set; }
    public bool IsDeleted { get; set; } = false;
    public string? Unit { get; set; }// For example, "kg", "cm", etc.
    public bool IsFilterable { get; set; } = false;
    public bool IsComparable { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public AttributeGroup AttributeGroup { get; set; } = null!;
    public ICollection<ProductAttributeTranslation> Translations { get; set; } = [];
    public ICollection<ProductAttributeValue> Values { get; set; } = [];
}

public enum AttributeType
{
    String,
    Decimal,
    Integer,
    Boolean
}