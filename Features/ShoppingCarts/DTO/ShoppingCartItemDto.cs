using LeveLEO.Features.Products.DTO;

namespace LeveLEO.Features.ShoppingCarts.DTO;

public class ShoppingCartItemDto
{
    public ProductResponseDto Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }

    /// <summary>Доступна кількість на складі (з урахуванням резервів).</summary>
    public int AvailableQuantity { get; set; }

    /// <summary>
    /// Кількість, яка бере участь у підсумках і оформленні замовлення.
    /// 0 якщо товар у кошику є, але зараз недоступний (залишок 0) — рядок лишається видимим.
    /// </summary>
    public int QuantityApplyingToTotals { get; set; }

    public bool IsExcludedFromPurchase => QuantityApplyingToTotals == 0;

    public decimal PriceAfterProductPromotion { get; set; }
    public decimal PriceAfterCartPromotion { get; set; }
    public decimal TotalPrice => PriceAfterCartPromotion * QuantityApplyingToTotals;
}