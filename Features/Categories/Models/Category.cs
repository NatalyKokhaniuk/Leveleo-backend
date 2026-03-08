using LeveLEO.Features.Products.Models;
using LeveLEO.Infrastructure.Translation.Models;
using LeveLEO.Models;

namespace LeveLEO.Features.Categories.Models;

public class Category : ITimestamped, ITranslatable<CategoryTranslation>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Slug { get; set; } = null!;
    public string? ImageKey { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Category? Parent { get; set; }
    public ICollection<CategoryTranslation> Translations { get; set; } = [];
    public ICollection<Category> Children { get; set; } = [];

    public ICollection<Product> Products { get; set; } = [];
    public ICollection<CategoryClosure> Ancestors { get; set; } = [];
    public ICollection<CategoryClosure> Descendants { get; set; } = [];
}