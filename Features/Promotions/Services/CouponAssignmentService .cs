using LeveLEO.Data;
using LeveLEO.Features.Promotions.DTO.Coupons;
using LeveLEO.Features.Promotions.Models.Coupons;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace LeveLEO.Features.Promotions.Services;

public class CouponAssignmentService(AppDbContext db) : ICouponAssignmentService
{
    public async Task<CouponAssignment> CreateAsync(CreateCouponAssigmentDto dto)
    {
        _ = await db.Promotions.FindAsync(dto.PromotionId) ?? throw new ApiException("PROMOTION_NOT_FOUND", $"Promotion with Id '{dto.PromotionId}' not found.", 404);

        _ = await db.Users.FindAsync(dto.UserId) ?? throw new ApiException("USER_NOT_FOUND", $"User with Id '{dto.UserId}' not found.", 404);
        var assignment = new CouponAssignment
        {
            PromotionId = dto.PromotionId,
            UserId = dto.UserId,
            MaxUsagePerUser = dto.MaxUsagePerUser.HasValue ? dto.MaxUsagePerUser.Value : null,
            ExpiresAt = dto.ExpiresAt.HasValue ? dto.ExpiresAt.Value : null
        };

        db.CouponAssignments.Add(assignment);
        await db.SaveChangesAsync();

        return assignment;
    }

    public async Task<List<CouponAssignment>> GetByUserAsync(string userId)
    {
        _ = await db.Users.FindAsync(userId) ?? throw new ApiException("USER_NOT_FOUND", $"User with Id '{userId}' not found.", 404);
        return await db.CouponAssignments
            .Include(ca => ca.Promotion)
            .Where(ca => ca.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<CouponAssignment>> GetByPromotionAsync(Guid promotionId)
    {
        _ = await db.Promotions.FindAsync(promotionId) ?? throw new ApiException("PROMOTION_NOT_FOUND", $"Promotion with Id '{promotionId}' not found.", 404);
        return await db.CouponAssignments
            .Include(ca => ca.Promotion)
            .Where(ca => ca.PromotionId == promotionId)
            .ToListAsync();
    }

    public async Task<List<CouponAssignment>> GetValidAssignmentsAsync(Guid promotionId, string userId)
    {
        _ = await db.Promotions.FindAsync(promotionId) ?? throw new ApiException("PROMOTION_NOT_FOUND", $"Promotion with Id '{promotionId}' not found.", 404);

        _ = await db.Users.FindAsync(userId) ?? throw new ApiException("USER_NOT_FOUND", $"User with Id '{userId}' not found.", 404);
        var now = DateTimeOffset.UtcNow;

        return await db.CouponAssignments
            .Where(ca => ca.PromotionId == promotionId &&
                         ca.UserId == userId &&
                         (!ca.ExpiresAt.HasValue || ca.ExpiresAt.Value >= now) &&
                         (!ca.MaxUsagePerUser.HasValue || ca.UsageCount < ca.MaxUsagePerUser.Value))
            .Include(ca => ca.Promotion)
            .ToListAsync();
    }

    public async Task<CouponAssignment?> GetNextValidAssignmentAsync(Guid promotionId, string userId)
    {
        var now = DateTimeOffset.UtcNow;

        return await db.CouponAssignments
            .Where(ca => ca.PromotionId == promotionId &&
                         ca.UserId == userId &&
                         (!ca.ExpiresAt.HasValue || ca.ExpiresAt.Value >= now) &&
                         (!ca.MaxUsagePerUser.HasValue || ca.UsageCount < ca.MaxUsagePerUser.Value))
            .OrderBy(ca => ca.ExpiresAt ?? DateTimeOffset.MaxValue)
            .FirstOrDefaultAsync();
    }

    public async Task<CouponAssignmentResultDto> IncrementUsageAsync(Guid assignmentId)
    {
        var assignment = await db.CouponAssignments.FindAsync(assignmentId);

        if (assignment == null)
            return new CouponAssignmentResultDto { Success = false, Message = "Assignment not found" };

        var now = DateTimeOffset.UtcNow;
        if (assignment.ExpiresAt.HasValue && assignment.ExpiresAt.Value < now)
            return new CouponAssignmentResultDto { Success = false, Message = "Coupon expired", Assignment = assignment };

        if (assignment.MaxUsagePerUser.HasValue && assignment.UsageCount >= assignment.MaxUsagePerUser.Value)
            return new CouponAssignmentResultDto { Success = false, Message = "Usage limit exceeded", Assignment = assignment };

        assignment.UsageCount++;
        await db.SaveChangesAsync();

        return new CouponAssignmentResultDto { Success = true, Assignment = assignment };
    }

    public async Task<CouponAssignmentResultDto> DeleteAsync(Guid assignmentId)
    {
        var assignment = await db.CouponAssignments.FindAsync(assignmentId);
        if (assignment == null)
            return new CouponAssignmentResultDto { Success = false, Message = "Assignment not found" };

        db.CouponAssignments.Remove(assignment);
        await db.SaveChangesAsync();

        return new CouponAssignmentResultDto { Success = true };
    }

    public async Task<CouponAssignmentResultDto> UpdateAsync(Guid assignmentId, UpdateCouponAssignmentDto dto)
    {
        var assignment = await db.CouponAssignments.FindAsync(assignmentId);
        if (assignment == null)
            return new CouponAssignmentResultDto { Success = false, Message = "Assignment not found" };

        if (dto.MaxUsagePerUser.HasValue)
            assignment.MaxUsagePerUser = dto.MaxUsagePerUser.Value;

        if (dto.ExpiresAt.HasValue)
            assignment.ExpiresAt = dto.ExpiresAt.Value;

        await db.SaveChangesAsync();

        return new CouponAssignmentResultDto { Success = true, Assignment = assignment };
    }
}