using LeveLEO.Models;

namespace LeveLEO.Features.Products.Models;

public class ProductVideo : ITimestamped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public string VideoKey { get; set; } = null!;
    public int SortOrder { get; set; }
    public Product Product { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}