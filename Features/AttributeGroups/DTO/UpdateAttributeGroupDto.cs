using LeveLEO.Infrastructure.Common;

namespace LeveLEO.Features.AttributeGroups.DTO;

/// <summary>
/// Partial update of attribute group
/// </summary>
public class UpdateAttributeGroupDto
{
    public Optional<string> Name { get; set; } = default;
    public Optional<string?> Description { get; set; } = default;
    public Optional<bool> IsActive { get; set; } = default;

    // Якщо AttributeGroup має переклади
    public Optional<List<CreateAttributeGroupTranslationDto>?> Translations { get; set; } = default;
}