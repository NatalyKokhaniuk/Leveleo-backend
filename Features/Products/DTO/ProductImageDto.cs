namespace LeveLEO.Features.Products.DTO;

public class ProductImageDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ImageKey { get; set; } = null!;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}