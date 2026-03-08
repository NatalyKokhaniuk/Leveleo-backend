using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.AttributeGroups.DTO;

/// <summary>
/// Translation for Attribute Group
/// </summary>
/// <example>
/// {
///   "languageCode": "uk",
///   "name": "Загальні",
///   "description": "Основні характеристики"
/// }
/// </example>
public class CreateAttributeGroupTranslationDto
{
    /// <summary>
    /// ISO language code (uk, en, pl, etc.)
    /// </summary>
    /// <example>uk</example>
    [Required]
    [MaxLength(5)]
    public string LanguageCode { get; set; } = null!;

    /// <summary>
    /// Localized name of attribute group
    /// </summary>
    /// <example>Загальні</example>
    [Required]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Localized description
    /// </summary>
    /// <example>Основні характеристики</example>
    public string? Description { get; set; }
}