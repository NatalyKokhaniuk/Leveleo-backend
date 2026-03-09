namespace LeveLEO.Features.Products.DTO;

public class ProductVideoDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string VideoKey { get; set; } = null!;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}