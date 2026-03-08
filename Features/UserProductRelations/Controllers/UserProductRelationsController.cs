using LeveLEO.Features.Products.DTO;
using LeveLEO.Features.UserProductRelations.DTO;
using LeveLEO.Features.UserProductRelations.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LeveLEO.Features.UserProductRelations.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserProductRelationsController(IUserProductRelationService service) : ControllerBase
{
    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new ApiException("USER_ID_NOT_FOUND", "User id not found in token", 401);
    }

    #region Favorites

    [HttpPost("{productId}/favorites")]
    public async Task<ActionResult<ProductRelationResultDto>> AddToFavorites(Guid productId)
    {
        var result = await service.AddToFavoritesAsync(productId, GetUserId());
        return Ok(result);
    }

    [HttpDelete("{productId}/favorites")]
    public async Task<ActionResult<ProductRelationResultDto>> RemoveFromFavorites(Guid productId)
    {
        var result = await service.RemoveFromFavoritesAsync(productId, GetUserId());
        return Ok(result);
    }

    [HttpGet("favorites/me")]
    public async Task<ActionResult<List<ProductResponseDto>>> GetMyFavorites()
    {
        var result = await service.GetFavoritesByUserIdAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("favorites/{userId}")]
    public async Task<ActionResult<List<ProductResponseDto>>> GetFavoritesByUser(string userId)
    {
        var result = await service.GetFavoritesByUserIdAsync(userId);
        return Ok(result);
    }

    #endregion Favorites

    #region Comparison

    [HttpPost("{productId}/comparison")]
    public async Task<ActionResult<ProductRelationResultDto>> AddToComparison(Guid productId)
    {
        var result = await service.AddToComparisonAsync(productId, GetUserId());
        return Ok(result);
    }

    [HttpDelete("{productId}/comparison")]
    public async Task<ActionResult<ProductRelationResultDto>> RemoveFromComparison(Guid productId)
    {
        var result = await service.RemoveFromComparisonAsync(productId, GetUserId());
        return Ok(result);
    }

    [HttpGet("comparison/me")]
    public async Task<ActionResult<List<ProductResponseDto>>> GetMyComparison()
    {
        var result = await service.GetComparisonByUserIdAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("comparison/{userId}")]
    public async Task<ActionResult<List<ProductResponseDto>>> GetComparisonByUser(string userId)
    {
        var result = await service.GetComparisonByUserIdAsync(userId);
        return Ok(result);
    }

    #endregion Comparison
}