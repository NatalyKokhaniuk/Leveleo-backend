namespace LeveLEO.Features.AttributeGroups.DTO;

/// <summary>
/// Attribute group response
/// </summary>
/// <example>
/// {
///   "id": "b3c9f7c1-5a6a-4e3c-9a9f-123456789abc",
///   "name": "General",
///   "slug": "general",
///   "description": "Main characteristics",
///   "translations": [
///     {
///       "id": "a1d2e3f4-1234-4abc-9def-987654321000",
///       "languageCode": "uk",
///       "name": "Загальні",
///       "description": "Основні характеристики"
///     }
///   ]
/// }
/// </example>
public class AttributeGroupResponseDto
{
    /// <example>b3c9f7c1-5a6a-4e3c-9a9f-123456789abc</example>
    public Guid Id { get; set; }

    /// <example>General</example>
    public string Name { get; set; } = null!;

    /// <example>general</example>
    public string Slug { get; set; } = null!;

    /// <example>Main characteristics</example>
    public string? Description { get; set; }

    public List<AttributeGroupTranslationResponseDto> Translations { get; set; } = [];
}