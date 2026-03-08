using LeveLEO.Infrastructure.Common;

namespace LeveLEO.Features.Promotions.DTO.Coupons;

public class CreateCouponAssigmentDto
{
    public Guid PromotionId { get; set; }
    public string UserId { get; set; }
    public Optional<int> MaxUsagePerUser { get; set; }
    public Optional<DateTimeOffset> ExpiresAt { get; set; }
}