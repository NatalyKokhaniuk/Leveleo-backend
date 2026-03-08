using LeveLEO.Features.Promotions.DTO.Coupons;
using LeveLEO.Features.Promotions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeveLEO.Features.Promotions.Controllers;

[ApiController]
[Route("api/promotions/coupon-assignments")]
[Authorize(Roles = "Admin,Moderator")]
public class CouponAssignmentsController(ICouponAssignmentService service) : ControllerBase
{
    // Отримати всі асігменти певного користувача
    [HttpGet("by-user/{userId}")]
    public async Task<IActionResult> GetByUser(string userId)
    {
        var assignments = await service.GetByUserAsync(userId);
        return Ok(assignments);
    }

    // Отримати всі асігменти певної промоакції
    [HttpGet("by-promotion/{promotionId:guid}")]
    public async Task<IActionResult> GetByPromotion(Guid promotionId)
    {
        var assignments = await service.GetByPromotionAsync(promotionId);
        return Ok(assignments);
    }

    // Створити новий асігмент
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCouponAssigmentDto dto)
    {
        var assignment = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetByUser), new { userId = assignment.UserId }, assignment);
    }

    // Оновити асігмент
    [HttpPut("{assignmentId:guid}")]
    public async Task<IActionResult> Update(Guid assignmentId, [FromBody] UpdateCouponAssignmentDto dto)
    {
        var result = await service.UpdateAsync(assignmentId, dto);
        if (!result.Success)
            return NotFound(new { result.Message });
        return Ok(result.Assignment);
    }

    // Видалити асігмент
    [HttpDelete("{assignmentId:guid}")]
    public async Task<IActionResult> Delete(Guid assignmentId)
    {
        var result = await service.DeleteAsync(assignmentId);
        if (!result.Success)
            return NotFound(new { result.Message });
        return NoContent();
    }
}