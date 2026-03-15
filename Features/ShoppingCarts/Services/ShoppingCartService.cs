using LeveLEO.Data;
using LeveLEO.Features.Inventory.Services;
using LeveLEO.Features.Products.DTO;
using LeveLEO.Features.Products.Models;
using LeveLEO.Features.Products.Services;
using LeveLEO.Features.Promotions.Services;
using LeveLEO.Features.ShoppingCarts.DTO;
using LeveLEO.Features.ShoppingCarts.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace LeveLEO.Features.ShoppingCarts.Services;

public class ShoppingCartService(
    AppDbContext db,
    IInventoryService inventory,
    IPromotionService promotion,
    IProductService productService) : IShoppingCartService
{
    #region GetCalculatedCart

    public async Task<ShoppingCartDto> GetCalculatedCartAsync(string userId)
    {
        var cart = await db.ShoppingCarts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            return new ShoppingCartDto { UserId = userId };
        }

        var removedItems = new List<ShoppingCartItemDto>();
        var CartAdjusted = false;
        foreach (var item in cart.Items.ToList())
        {
            var available = await inventory.GetAvailableQuantityAsync(item.ProductId);

            if (available == 0)
            {
                cart.Items.Remove(item);
                removedItems.Add(await MapToDtoAsync(item));
                db.ShoppingCartItems.Remove(item);
            }
            else if (item.Quantity > available)
            {
                item.Quantity = available;
                CartAdjusted = true;
            }
        }

        await db.SaveChangesAsync();

        var itemDtos = await Task.WhenAll(cart.Items.Select(MapToDtoAsync));
        var promoResult = await promotion.ApplyCartPromotionAsync(itemDtos, cart.CouponCode, userId);

        return new ShoppingCartDto
        {
            Id = cart.Id,
            UserId = userId,
            CouponCode = cart.CouponCode,
            Items = promoResult.Items,
            TotalOriginalPrice = promoResult.TotalProductsPrice,
            TotalProductDiscount = promoResult.TotalProductDiscount,
            TotalCartDiscount = promoResult.TotalCartDiscount,
            TotalPayable = promoResult.FinalPrice,
            AppliedCartPromotion = promoResult.AppliedCartPromotion,
            RemovedItems = removedItems,
            CartAdjusted = CartAdjusted
        };
    }

    #endregion GetCalculatedCart

    #region Add/Increase/Decrease/Remove

    public async Task<ShoppingCartItemDto> AddItemAsync(string userId, Guid productId, int quantity)
    {
        var available = await inventory.GetAvailableQuantityAsync(productId);
        if (quantity > available)
            throw new ApiException("INSUFFICIENT_STOCK", $"Only {available} units are available.", 400);

        var cart = await GetOrCreateCartAsync(userId);

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            if (existingItem.Quantity + quantity > available)
                throw new ApiException("INSUFFICIENT_STOCK", $"Only {available} units are available.", 400);
            existingItem.Quantity += quantity;
        }
        else
        {
            var product = await productService.GetByIdAsync(productId) ?? throw new ApiException("PRODUCT_NOT_FOUND", $"Product with Id '{productId}' not found.", 404);
            var discontedPrice = product.DiscountedPrice ?? product.Price;

            var item = new ShoppingCartItem
            {
                CartId = cart.Id,
                ProductId = productId,
                Quantity = quantity,
                PriceAfterProductPromotion = discontedPrice, 
                PriceAfterCartPromotion = discontedPrice
            };
            cart.Items.Add(item);
            db.ShoppingCartItems.Add(item);
        }

        await db.SaveChangesAsync();

        return await MapToDtoAsync(cart.Items.First(i => i.ProductId == productId));
    }

    public async Task<ShoppingCartItemDto> IncreaseQuantityAsync(string userId, Guid productId, int amount = 1)
    {
        var cart = await GetOrCreateCartAsync(userId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new ApiException("ITEM_NOT_FOUND", $"Item not found in cart.", 404);

        var available = await inventory.GetAvailableQuantityAsync(productId);
        if (item.Quantity + amount > available)
            throw new ApiException("INSUFFICIENT_STOCK", $"Only {available} units available.", 400);

        item.Quantity += amount;
        await db.SaveChangesAsync();

        return await MapToDtoAsync(item);
    }

    public async Task<ShoppingCartItemDto?> DecreaseQuantityAsync(string userId, Guid productId, int amount = 1)
    {
        var cart = await GetOrCreateCartAsync(userId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            return null;

        item.Quantity -= amount;

        if (item.Quantity <= 0)
        {
            db.ShoppingCartItems.Remove(item);
            cart.Items.Remove(item);
        }

        await db.SaveChangesAsync();
        return item.Quantity > 0 ? await MapToDtoAsync(item) : null;
    }

    public async Task RemoveItemAsync(string userId, Guid productId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            cart.Items.Remove(item);
            db.ShoppingCartItems.Remove(item);
            await db.SaveChangesAsync();
        }
    }

    #endregion Add/Increase/Decrease/Remove

    #region ApplyCoupon / RemoveCoupon / ClearCart

    public async Task<ShoppingCartDto> ApplyCouponAsync(string userId, string couponCode)
    {
        var cart = await db.ShoppingCarts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId) ?? throw new ApiException("CART_NOT_FOUND", "Shopping cart not found.", 404);

        cart.CouponCode = couponCode;
        await db.SaveChangesAsync();
        return await GetCalculatedCartAsync(userId);
    }

    public async Task<ShoppingCartDto> RemoveCouponAsync(string userId)
    {
        var cart = await db.ShoppingCarts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId) ?? throw new ApiException("CART_NOT_FOUND", "Shopping cart not found.", 404);

        cart.CouponCode = null;
        await db.SaveChangesAsync();
        return await GetCalculatedCartAsync(userId);
    }

    public async Task<CartClearResultDto> ClearCartAsync(string userId)
    {
        var cart = await db.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart != null)
        {
            db.ShoppingCartItems.RemoveRange(cart.Items);
            cart.Items.Clear();
            cart.CouponCode = null;
            await db.SaveChangesAsync();
        }

        return new CartClearResultDto
        {
            Success = true,
            Message = "The shopping cart has been successfully emptied"
        };
    }

    #endregion ApplyCoupon / RemoveCoupon / ClearCart

    #region Helpers

    private async Task<ShoppingCart> GetOrCreateCartAsync(string userId)
    {
        var cart = await db.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new ShoppingCart
            {
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.ShoppingCarts.Add(cart);
            await db.SaveChangesAsync();
        }

        return cart;
    }

    private async Task<ShoppingCartItemDto> MapToDtoAsync(ShoppingCartItem item)
    {
        var productDto = await productService.BuildFullDtoAsync(item.ProductId);

        return new ShoppingCartItemDto
        {
            Product = productDto,
            Quantity = item.Quantity,
            Price = item.PriceAfterProductPromotion,
            PriceAfterProductPromotion = item.PriceAfterProductPromotion,
            PriceAfterCartPromotion = item.PriceAfterCartPromotion
        };
    }

    #endregion Helpers
}
