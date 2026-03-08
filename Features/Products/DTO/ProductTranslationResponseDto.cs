namespace LeveLEO.Features.Products.DTO;

public class ProductTranslationResponseDto
{
    public Guid Id { get; set; }
    public string LanguageCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}