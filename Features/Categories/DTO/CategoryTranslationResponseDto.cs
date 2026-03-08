namespace LeveLEO.Features.Categories.DTO;

/// <summary>
/// DTO перекладу відповіді
/// </summary>
public class CategoryTranslationResponseDto
{
    public Guid Id { get; set; }
    public string LanguageCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}