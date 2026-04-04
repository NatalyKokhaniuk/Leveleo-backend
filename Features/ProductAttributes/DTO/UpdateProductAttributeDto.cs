using LeveLEO.Features.ProductAttributes.Models;
using LeveLEO.Infrastructure.Common;

namespace LeveLEO.Features.ProductAttributes.DTO;

public class UpdateProductAttributeDto
{
    public Optional<string> Name { get; set; } = default;
    public Optional<Guid> AttributeGroupId { get; set; } = default;
    public Optional<string?> Description { get; set; } = default;
    public Optional<AttributeType> Type { get; set; } = default;
    public Optional<string?> Unit { get; set; } = default;
    public Optional<bool> IsFilterable { get; set; } = default;
    public Optional<bool> IsComparable { get; set; } = default;

    // Переклади можна оновлювати одночасно
    public Optional<List<CreateProductAttributeTranslationDto>?> Translations { get; set; } = default;
}