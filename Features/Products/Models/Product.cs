using LeveLEO.Features.Brands.Models;
using LeveLEO.Features.Categories.Models;
using LeveLEO.Features.Orders.Models;
using LeveLEO.Features.ProductAttributes.Models;
using LeveLEO.Features.ProductAttributeValues.Models;
using LeveLEO.Features.ShoppingCarts.Models;
using LeveLEO.Features.UserProductRelations.Models;
using LeveLEO.Infrastructure.Translation.Models;
using LeveLEO.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeveLEO.Features.Products.Models;

public class Product : ITimestamped, ITranslatable<ProductTranslation>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? MainImageKey { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CategoryId { get; set; }
    public Guid BrandId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Category Category { get; set; } = null!;
    public Brand Brand { get; set; } = null!;
    public ICollection<ProductTranslation> Translations { get; set; } = [];

    public ICollection<ProductAttributeValue> AttributeValues { get; set; } = [];
    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<ProductVideo> Videos { get; set; } = [];
    public ICollection<UserFavorite> FavoritedBy { get; set; } = [];
    public ICollection<UserComparison> ComparedBy { get; set; } = [];
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}