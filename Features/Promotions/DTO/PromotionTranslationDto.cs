namespace LeveLEO.Features.Promotions.DTO;

public class PromotionTranslationDto
{
    public string LanguageCode { get; set; } = null!;   // "en", "uk", "pl" і т.д.

    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}