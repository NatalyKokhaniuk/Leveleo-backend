using System;
using System.Collections.Generic;
using LeveLEO.Infrastructure.Common;

namespace LeveLEO.Features.ProductAttributeValues.DTO;

public class UpdateProductAttributeValueDto
{
    public Optional<Guid> ProductAttributeId { get; set; } = default;
    public Optional<string?> StringValue { get; set; } = default;
    public Optional<decimal?> DecimalValue { get; set; } = default;
    public Optional<int?> IntValue { get; set; } = default;
    public Optional<bool?> BoolValue { get; set; } = default;
    public Optional<List<CreateProductAttributeValueTranslationDto>?> Translations { get; set; } = default;
}