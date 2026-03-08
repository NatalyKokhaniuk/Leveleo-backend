using LeveLEO.Features.Promotions.Models;
using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.Promotions.DTO;

public class AppliedPromotionDto
{
    public Guid Id { get; set; }

    public string Slug { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageKey { get; set; }
    public PromotionLevel Level { get; set; }

    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public bool IsCoupon { get; set; } = false;

    public bool IsPersonal { get; set; } = false;
    public string? CouponCode { get; set; }
    public ICollection<PromotionTranslation> Translations { get; set; } = [];
}