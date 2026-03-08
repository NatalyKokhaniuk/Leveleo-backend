using LeveLEO.Features.Products.DTO;

namespace LeveLEO.Features.ShoppingCarts.DTO;

public class ShoppingCartItemDto
{
    public ProductResponseDto Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }

    public decimal PriceAfterProductPromotion { get; set; }
    public decimal PriceAfterCartPromotion { get; set; }
    public decimal TotalPrice => PriceAfterCartPromotion * Quantity;
}