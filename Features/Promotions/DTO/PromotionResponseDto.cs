using LeveLEO.Features.Promotions.Models;

namespace LeveLEO.Features.Promotions.DTO;

public class PromotionResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageKey { get; set; }

    public PromotionLevel Level { get; set; }

    public DiscountType? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }

    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }

    public bool IsActive { get; set; }

    public bool IsCoupon { get; set; }
    public bool IsPersonal { get; set; }

    public ICollection<PromotionTranslationDto> Translations { get; set; } = [];
}