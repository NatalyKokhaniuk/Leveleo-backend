using LeveLEO.Features.ProductAttributes.Models;
using LeveLEO.Infrastructure.Translation.Models;
using LeveLEO.Models;

namespace LeveLEO.Features.AttributeGroups.Models;

public class AttributeGroup : ITimestamped, ITranslatable<AttributeGroupTranslation>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<AttributeGroupTranslation> Translations { get; set; } = [];
    public ICollection<ProductAttribute> Attributes { get; set; } = [];
}