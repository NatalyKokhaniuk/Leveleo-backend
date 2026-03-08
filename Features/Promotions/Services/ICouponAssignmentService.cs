using LeveLEO.Features.Promotions.DTO.Coupons;
using LeveLEO.Features.Promotions.Models.Coupons;

namespace LeveLEO.Features.Promotions.Services;

public interface ICouponAssignmentService
{
    Task<CouponAssignment> CreateAsync(CreateCouponAssigmentDto dto);

    Task<List<CouponAssignment>> GetByUserAsync(string userId);

    Task<List<CouponAssignment>> GetByPromotionAsync(Guid promotionId);

    Task<CouponAssignmentResultDto> IncrementUsageAsync(Guid assignmentId);

    Task<CouponAssignmentResultDto> DeleteAsync(Guid assignmentId);

    Task<CouponAssignmentResultDto> UpdateAsync(Guid assignmentId, UpdateCouponAssignmentDto dto);

    Task<CouponAssignment?> GetNextValidAssignmentAsync(Guid promotionId, string userId);

    Task<List<CouponAssignment>> GetValidAssignmentsAsync(Guid promotionId, string userId);
}