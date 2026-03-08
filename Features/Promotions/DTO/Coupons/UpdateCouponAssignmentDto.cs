using LeveLEO.Infrastructure.Common;

namespace LeveLEO.Features.Promotions.DTO.Coupons;

public class UpdateCouponAssignmentDto
{
    public Optional<int> MaxUsagePerUser { get; set; }
    public Optional<DateTimeOffset> ExpiresAt { get; set; }
}