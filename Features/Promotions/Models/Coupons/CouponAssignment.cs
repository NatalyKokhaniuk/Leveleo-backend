using LeveLEO.Features.Identity.Models;
using LeveLEO.Infrastructure.Common;
using LeveLEO.Models;
using System.ComponentModel.DataAnnotations;

namespace LeveLEO.Features.Promotions.Models.Coupons;

public class CouponAssignment : ITimestamped
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PromotionId { get; set; }
    public Promotion Promotion { get; set; } = null!;

    [Required]
    public string UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public int UsageCount { get; set; } = 0;
    public int? MaxUsagePerUser { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}