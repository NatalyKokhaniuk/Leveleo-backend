namespace LeveLEO.Features.Brands.DTO;

public class BrandResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Slug { get; set; } = null!;
    public string? LogoKey { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public List<BrandTranslationResponseDto> Translations { get; set; } = [];
}