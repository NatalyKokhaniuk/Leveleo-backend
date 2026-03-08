using LeveLEO.Infrastructure.Common;

namespace LeveLEO.Features.Promotions.Models.LevelConditions;

public class CartLevelCondition
{
    public Optional<decimal> MinTotalAmount { get; set; }
    public Optional<int> MinQuantity { get; set; }

    public Optional<List<Guid>> ProductIds { get; set; }
    public Optional<List<Guid>> CategoryIds { get; set; }
}