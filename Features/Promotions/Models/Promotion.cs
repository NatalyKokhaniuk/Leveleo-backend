using LeveLEO.Features.Promotions.Models.Coupons;
using LeveLEO.Features.Promotions.Models.LevelConditions;
using LeveLEO.Infrastructure.Translation.Models;
using LeveLEO.Models;

namespace LeveLEO.Features.Promotions.Models;

public class Promotion : ITimestamped, ITranslatable<PromotionTranslation>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageKey { get; set; }

    //знижка рівня товар, кошик чи доставка
    public PromotionLevel Level { get; set; }

    public ProductLevelCondition? ProductConditions { get; set; }
    public CartLevelCondition? CartConditions { get; set; }

    //дисконт %% чи фіксована сума і рівень знижки
    public DiscountType? DiscountType { get; set; }

    public decimal? DiscountValue { get; set; }

    //термін дії акції
    public DateTimeOffset StartDate { get; set; }

    public DateTimeOffset EndDate { get; set; }

    public bool IsActive =>
    DateTimeOffset.UtcNow >= StartDate &&
    DateTimeOffset.UtcNow <= EndDate;

    //чи потрібен купон для акції, якщо так, то який код купона і скільки разів він може бути використаний
    public bool IsCoupon { get; set; } = false;

    public bool IsPersonal { get; set; } = false;
    public string? CouponCode { get; set; }
    public int? MaxUsages { get; set; }
    public int UsedCount { get; set; } = 0;
    public ICollection<CouponAssignment> Assignments { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<PromotionTranslation> Translations { get; set; } = [];
}

public enum PromotionLevel
{
    Product,
    Cart
}

public enum DiscountType
{
    Percentage,
    FixedAmount
}

public enum CouponStatus
{
    NotProvided,
    Applied,
    Invalid,
    Expired,
    NotAssignedToUser,
    UsageLimitExceeded,
    ConditionsNotMet
}