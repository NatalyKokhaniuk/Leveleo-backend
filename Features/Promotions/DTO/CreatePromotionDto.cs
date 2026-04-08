using LeveLEO.Features.Promotions.Models;
using LeveLEO.Features.Promotions.Models.LevelConditions;

namespace LeveLEO.Features.Promotions.DTO;

public class CreatePromotionDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageKey { get; set; }

    public PromotionLevel Level { get; set; }

    public ProductLevelCondition? ProductConditions { get; set; }
    public CartLevelCondition? CartConditions { get; set; }

    public DiscountType? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }

    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }

    public bool IsCoupon { get; set; }
    public bool IsPersonal { get; set; }
    public string? CouponCode { get; set; }
    public int? MaxUsages { get; set; }

    public ICollection<PromotionTranslationDto>? Translations { get; set; }
}