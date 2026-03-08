using LeveLEO.Infrastructure.Common;

namespace LeveLEO.Features.Brands.DTO;

/// <summary>
/// Partial update of brand
/// </summary>
public class UpdateBrandDto
{
    public Optional<string> Name { get; set; } = default;
    public Optional<string?> Description { get; set; } = default;
    public Optional<string?> LogoKey { get; set; } = default;
    public Optional<string?> MetaTitle { get; set; } = default;
    public Optional<string?> MetaDescription { get; set; } = default;

    public Optional<List<CreateBrandTranslationDto>?> Translations { get; set; } = default;
}