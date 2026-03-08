using LeveLEO.Infrastructure.Translation.Models;
using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.AttributeGroups.Models;

public class AttributeGroupTranslation : ITranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid AttributeGroupId { get; set; }

    [Required]
    [MaxLength(5)]
    public string LanguageCode { get; set; } = null!;

    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public AttributeGroup AttributeGroup { get; set; } = null!;
}