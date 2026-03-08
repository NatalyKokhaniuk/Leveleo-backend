using LeveLEO.Infrastructure.Common;

namespace LeveLEO.Features.Products.DTO;

public class AttributeFilterValueDto
{
    public Guid AttributeId { get; set; }

    public Optional<List<string>> StringValues { get; set; }
    public Optional<List<decimal>> DecimalValues { get; set; }
    public Optional<List<int>> IntegerValues { get; set; }
    public Optional<List<bool>> BooleanValues { get; set; }
}