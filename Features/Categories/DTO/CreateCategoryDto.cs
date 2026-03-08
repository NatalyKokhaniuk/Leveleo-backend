using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.Categories.DTO;

/// <summary>
/// DTO для створення/редагування категорії
/// </summary>
public class CreateCategoryDto
{
    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public bool IsActive { get; set; } = true;

    public List<CreateCategoryTranslationDto>? Translations { get; set; }
}