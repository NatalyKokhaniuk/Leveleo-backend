namespace LeveLEO.Features.Brands.DTO;

public class BrandTranslationResponseDto
{
    public Guid Id { get; set; }
    public string LanguageCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}