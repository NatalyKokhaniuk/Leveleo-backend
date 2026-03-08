using LeveLEO.Features.Products.Models;
using LeveLEO.Infrastructure.Translation.Models;
using LeveLEO.Models;

namespace LeveLEO.Features.Brands.Models;

public class Brand : ITimestamped, ITranslatable<BrandTranslation>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Slug { get; set; } = null!;
    public string? LogoKey { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<BrandTranslation> Translations { get; set; } = new List<BrandTranslation>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}