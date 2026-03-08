using LeveLEO.Features.Promotions.Models;
using LeveLEO.Features.Promotions.Models.LevelConditions;
using LeveLEO.Infrastructure.Common;

namespace LeveLEO.Features.Promotions.DTO;

public class UpdatePromotionDto
{
    public Optional<string> Name { get; set; }
    public Optional<string?> Description { get; set; }
    public Optional<string?> ImageKey { get; set; }

    public Optional<PromotionLevel> Level { get; set; }

    public Optional<ProductLevelCondition?> ProductConditions { get; set; }
    public Optional<CartLevelCondition?> CartConditions { get; set; }

    public Optional<DiscountType?> DiscountType { get; set; }
    public Optional<decimal?> DiscountValue { get; set; }

    public Optional<DateTimeOffset> StartDate { get; set; }
    public Optional<DateTimeOffset> EndDate { get; set; }

    public Optional<bool> IsCoupon { get; set; }
    public Optional<bool> IsPersonal { get; set; }
    public Optional<string?> CouponCode { get; set; }
    public Optional<int?> MaxUsages { get; set; }
}