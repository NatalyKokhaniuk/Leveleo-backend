namespace LeveLEO.Features.Products.DTO;

public class ProductTranslationDto
{
    public string LanguageCode { get; set; } = "uk";
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}