using LeveLEO.Features.ShoppingCarts.DTO;
using LeveLEO.Features.ShoppingCarts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LeveLEO.Features.ShoppingCarts.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShoppingCartController(IShoppingCartService service) : ControllerBase
{
    #region Admin Access

    [HttpGet("{userId}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<ShoppingCartDto>> GetCartByUserId(string userId)
    {
        var cart = await service.GetCalculatedCartAsync(userId);
        return Ok(cart);
    }

    #endregion Admin Access

    #region User Cart Operations

    // Отримати свій кошик
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ShoppingCartDto>> GetMyCart()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new ApiException("UNAUTHORIZED", "Unauthorized", 401);

        var cart = await service.GetCalculatedCartAsync(userId);
        return Ok(cart);
    }

    [HttpPost("items")]
    [Authorize]
    public async Task<ActionResult<ShoppingCartItemDto>> AddItem([FromBody] AddItemDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new ApiException("UNAUTHORIZED", "Unauthorized", 401);

        var item = await service.AddItemAsync(userId, dto.ProductId, dto.Quantity);
        return Ok(item);
    }

    [HttpPost("items/{productId}/increase")]
    [Authorize]
    public async Task<ActionResult<ShoppingCartItemDto>> IncreaseItem(Guid productId, [FromQuery] int amount = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new ApiException("UNAUTHORIZED", "Unauthorized", 401);

        var item = await service.IncreaseQuantityAsync(userId, productId, amount);
        return Ok(item);
    }

    [HttpPost("items/{productId}/decrease")]
    [Authorize]
    public async Task<ActionResult<ShoppingCartItemDto?>> DecreaseItem(Guid productId, [FromQuery] int amount = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new ApiException("UNAUTHORIZED", "Unauthorized", 401);

        var item = await service.DecreaseQuantityAsync(userId, productId, amount);
        return Ok(item);
    }

    [HttpDelete("items/{productId}")]
    [Authorize]
    public async Task<IActionResult> RemoveItem(Guid productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new ApiException("UNAUTHORIZED", "Unauthorized", 401);

        await service.RemoveItemAsync(userId, productId);
        return NoContent();
    }

    #endregion User Cart Operations

    #region Coupon Operations

    [HttpPost("coupon")]
    [Authorize]
    public async Task<ActionResult<ShoppingCartDto>> ApplyCoupon([FromBody] ApplyCouponDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new ApiException("UNAUTHORIZED", "Unauthorized", 401);

        var cart = await service.ApplyCouponAsync(userId, dto.CouponCode);
        return Ok(cart);
    }

    [HttpDelete("coupon")]
    [Authorize]
    public async Task<ActionResult<ShoppingCartDto>> RemoveCoupon()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new ApiException("UNAUTHORIZED", "Unauthorized", 401);

        var cart = await service.RemoveCouponAsync(userId);
        return Ok(cart);
    }

    #endregion Coupon Operations

    #region Clear Cart

    [HttpDelete("clear")]
    [Authorize]
    public async Task<ActionResult<CartClearResultDto>> ClearCart()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new ApiException("UNAUTHORIZED", "Unauthorized", 401);

        var result = await service.ClearCartAsync(userId);
        return Ok(result);
    }

    #endregion Clear Cart
}

// Допоміжні DTO
public record AddItemDto
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}

public record ApplyCouponDto
{
    public string CouponCode { get; init; } = string.Empty;
}