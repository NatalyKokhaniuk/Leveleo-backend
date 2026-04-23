using LeveLEO.Features.Products.Models;
using LeveLEO.Features.Promotions.DTO;
using LeveLEO.Features.Promotions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace LeveLEO.Features.Promotions.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PromotionsController(IPromotionService service) : ControllerBase
{
    private bool IncludeSensitiveCouponFields =>
        User.Identity?.IsAuthenticated == true &&
        (User.IsInRole("Admin") || User.IsInRole("Moderator"));

    #region CRUD

    [HttpPost]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<PromotionResponseDto>> Create([FromBody] CreatePromotionDto dto)
    {
        var result = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<PromotionResponseDto>> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id, IncludeSensitiveCouponFields);
        return Ok(result);
    }

    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<PromotionResponseDto>> GetBySlug(string slug)
    {
        var result = await service.GetBySlugAsync(slug, IncludeSensitiveCouponFields);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<PromotionResponseDto>> Update(Guid id, [FromBody] UpdatePromotionDto dto)
    {
        var result = await service.UpdateAsync(id, dto);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Активні акції за датами. Без авторизації.
    /// <paramref name="guestEligibleOnly"/> = true — тільки такі, що не потребують купона та не персональні (для гостей і вітрини).
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<List<PromotionResponseDto>>> GetActive([FromQuery] bool guestEligibleOnly = false)
    {
        var result = await service.GetActiveAsync(guestEligibleOnly);
        return Ok(result);
    }

    [HttpGet("")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<List<PromotionResponseDto>>> GetAll()
    {
        var result = await service.GetAllAsync();
        return Ok(result);
    }

    #endregion CRUD

    #region TRANSLATIONS

    [HttpPost("{promotionId:guid}/translations")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<PromotionTranslationDto>> AddTranslation(Guid promotionId, [FromBody] PromotionTranslationDto dto)
    {
        var result = await service.AddTranslationAsync(promotionId, dto);
        return CreatedAtAction(nameof(GetTranslationById), new { promotionId, languageCode = result.LanguageCode }, result);
    }

    [HttpPut("{promotionId:guid}/translations")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<PromotionTranslationDto>> UpdateTranslation(Guid promotionId, [FromBody] PromotionTranslationDto dto)
    {
        var result = await service.UpdateTranslationAsync(promotionId, dto);
        return Ok(result);
    }

    [HttpDelete("{promotionId:guid}/translations/{languageCode}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> DeleteTranslation(Guid promotionId, string languageCode)
    {
        await service.DeleteTranslationAsync(promotionId, languageCode);
        return NoContent();
    }

    [HttpGet("{promotionId:guid}/translations")]
    [AllowAnonymous]
    public async Task<ActionResult<List<PromotionTranslationDto>>> GetTranslations(Guid promotionId)
    {
        var promotion = await service.GetByIdAsync(promotionId, IncludeSensitiveCouponFields);
        return Ok(promotion.Translations);
    }

    [HttpGet("{promotionId:guid}/translations/{languageCode}")]
    [AllowAnonymous]
    public async Task<ActionResult<PromotionTranslationDto>> GetTranslationById(Guid promotionId, string languageCode)
    {
        var promotion = await service.GetByIdAsync(promotionId, IncludeSensitiveCouponFields);
        var normalizedLanguageCode = languageCode.Trim().ToLowerInvariant();
        var translation = promotion.Translations.FirstOrDefault(t => t.LanguageCode.ToLowerInvariant() == normalizedLanguageCode);
        if (translation == null)
            return NotFound();
        return Ok(translation);
    }

    #endregion TRANSLATIONS
}