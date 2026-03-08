using System;
using System.Collections.Generic;
using LeveLEO.Infrastructure.Common;

namespace LeveLEO.Features.Products.DTO;

public class UpdateProductDto
{
    public Optional<string> Name { get; set; } = default;
    public Optional<string?> Description { get; set; } = default;
    public Optional<decimal> Price { get; set; } = default;
    public Optional<Guid> CategoryId { get; set; } = default;
    public Optional<Guid> BrandId { get; set; } = default;
    public Optional<string?> MainImageKey { get; set; } = default;
    public Optional<int> StockQuantity { get; set; } = default;
    public Optional<bool> IsActive { get; set; } = default;
    public Optional<List<ProductTranslationDto>?> Translations { get; set; } = default;
}