using LeveLEO.Features.ProductAttributes.Models;
using LeveLEO.Features.Products.Models;
using LeveLEO.Infrastructure.Translation.Models;
using LeveLEO.Models;

namespace LeveLEO.Features.ProductAttributeValues.Models;

public class ProductAttributeValue : ITimestamped, ITranslatable<ProductAttributeValueTranslation>
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid ProductAttributeId { get; set; }
    public ProductAttribute ProductAttribute { get; set; } = null!;

    // Саме значення. Для simplicity -  все як string, int, decimal, bool
    public string? StringValue { get; set; }

    public decimal? DecimalValue { get; set; }
    public int? IntValue { get; set; }
    public bool? BoolValue { get; set; }

    public ICollection<ProductAttributeValueTranslation> Translations { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
