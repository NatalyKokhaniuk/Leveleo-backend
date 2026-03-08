using LeveLEO.Features.Promotions.Models.Coupons;

namespace LeveLEO.Features.Promotions.DTO.Coupons;

public class CouponAssignmentResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public CouponAssignment? Assignment { get; set; }
}