namespace LeveLEO.Features.AttributeGroups.DTO;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Create attribute group
/// </summary>
/// <example>
/// {
///   "name": "General",
///   "slug": "general",
///   "description": "Main characteristics",
///   "translations": [
///     {
///       "languageCode": "uk",
///       "name": "Загальні",
///       "description": "Основні характеристики"
///     }
///   ]
/// }
/// </example>
public class CreateAttributeGroupDto
{
    /// <summary>
    /// Default name (fallback language)
    /// </summary>
    /// <example>General</example>
    [Required]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Default description
    /// </summary>
    /// <example>Main characteristics</example>
    public string? Description { get; set; }

    /// <summary>
    /// Optional translations
    /// </summary>
    public List<CreateAttributeGroupTranslationDto>? Translations { get; set; }
}