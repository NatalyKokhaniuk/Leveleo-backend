using LeveLEO.Infrastructure.Translation.Models;
using LeveLEO.Models;

namespace LeveLEO.Features.Products.Models;

public class ProductTranslation : ITimestamped, ITranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public string LanguageCode { get; set; } = "uk";
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public Product Product { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}