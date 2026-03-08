using LeveLEO.Features.Products.DTO;
using LeveLEO.Features.Promotions.DTO;

namespace LeveLEO.Features.ShoppingCarts.DTO;

public class ShoppingCartDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public string? CouponCode { get; set; }
    public AppliedPromotionDto? AppliedCartPromotion { get; set; } // Акція рівня кошика, якщо застосована

    public List<ShoppingCartItemDto> Items { get; set; } = [];
    public List<ShoppingCartItemDto> RemovedItems { get; set; } = [];
    public bool CartAdjusted { get; set; }

    // Суми
    public decimal TotalOriginalPrice { get; set; }     // сума всіх товарів без знижок

    public decimal TotalProductDiscount { get; set; }   // сума знижок продуктового рівня
    public decimal TotalCartDiscount { get; set; }      // сума знижки кошика
    public decimal TotalPayable { get; set; }           // фінальна сума до оплати
}