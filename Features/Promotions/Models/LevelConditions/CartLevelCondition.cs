using LeveLEO.Infrastructure.Common;

namespace LeveLEO.Features.Promotions.Models.LevelConditions;

public class CartLevelCondition
{
    public decimal? MinTotalAmount { get; set; }
    public int? MinQuantity { get; set; }

    public Optional<List<Guid>> ProductIds { get; set; }
    public Optional<List<Guid>> CategoryIds { get; set; }

    public decimal GetMinTotalAmountOrZero() => MinTotalAmount ?? 0m;

    public int GetMinQuantityOrZero() => MinQuantity ?? 0;

    public List<Guid>? GetProductIdsOrEmpty() => ProductIds.HasValue ? ProductIds.Value : [];

    public List<Guid>? GetCategoryIdsOrEmpty() => CategoryIds.HasValue ? CategoryIds.Value : [];
}