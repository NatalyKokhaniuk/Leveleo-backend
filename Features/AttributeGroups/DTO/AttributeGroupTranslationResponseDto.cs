namespace LeveLEO.Features.AttributeGroups.DTO;

/// <summary>
/// Attribute group translation response
/// </summary>
/// <example>
/// {
///   "id": "a1d2e3f4-1234-4abc-9def-987654321000",
///   "languageCode": "uk",
///   "name": "Загальні",
///   "description": "Основні характеристики"
/// }
/// </example>
public class AttributeGroupTranslationResponseDto
{
    /// <summary>
    /// Translation identifier
    /// </summary>
    /// <example>a1d2e3f4-1234-4abc-9def-987654321000</example>
    public Guid Id { get; set; }

    /// <summary>
    /// ISO language code (uk, en, pl, etc.)
    /// </summary>
    /// <example>uk</example>
    public string LanguageCode { get; set; } = null!;

    /// <summary>
    /// Localized name
    /// </summary>
    /// <example>Загальні</example>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Localized description
    /// </summary>
    /// <example>Основні характеристики</example>
    public string? Description { get; set; }
}