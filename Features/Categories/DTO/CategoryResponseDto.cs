namespace LeveLEO.Features.Categories.DTO;

/// <summary>
/// DTO відповіді по категорії
/// </summary>
public class CategoryResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Slug { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public bool IsActive { get; set; }
    public string? ImageKey { get; set; }
    public string FullPath { get; set; } = string.Empty;
    public List<CategoryTranslationResponseDto> Translations { get; set; } = [];
}