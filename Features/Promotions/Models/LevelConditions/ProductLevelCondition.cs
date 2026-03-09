using LeveLEO.Infrastructure.Common;

namespace LeveLEO.Features.Promotions.Models.LevelConditions;

public class ProductLevelCondition
{
    public Optional<List<Guid>> ProductIds { get; set; }
    public Optional<List<Guid>> CategoryIds { get; set; }

    public List<Guid>? GetProductIdsOrEmpty() => ProductIds.HasValue ? ProductIds.Value : [];

    public List<Guid>? GetCategoryIdsOrEmpty() => CategoryIds.HasValue ? CategoryIds.Value : [];
}