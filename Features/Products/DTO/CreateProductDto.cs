namespace LeveLEO.Features.Products.DTO;

public class CreateProductDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public Guid CategoryId { get; set; }
    public Guid BrandId { get; set; }
    public string? MainImageKey { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ProductTranslationDto>? Translations { get; set; }
}