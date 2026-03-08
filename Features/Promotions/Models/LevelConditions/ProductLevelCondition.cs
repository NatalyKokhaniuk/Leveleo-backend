using LeveLEO.Infrastructure.Common;

namespace LeveLEO.Features.Promotions.Models.LevelConditions;

public class ProductLevelCondition
{
    public Optional<List<Guid>> ProductIds { get; set; }
    public Optional<List<Guid>> CategoryIds { get; set; }
}