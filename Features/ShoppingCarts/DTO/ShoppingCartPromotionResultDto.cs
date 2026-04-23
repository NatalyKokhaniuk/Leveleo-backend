using LeveLEO.Features.Promotions.DTO;

namespace LeveLEO.Features.ShoppingCarts.DTO;

public class ShoppingCartPromotionResultDto
{
    public List<ShoppingCartItemDto> Items { get; set; } = [];

    public decimal TotalProductsPrice { get; set; }
    public decimal TotalProductDiscount { get; set; }
    public decimal TotalCartDiscount { get; set; }
    public decimal FinalPrice { get; set; }

    /// <summary>
    /// Акція рівня кошика, яка була застосована (може бути null)
    /// </summary>
    public AppliedPromotionDto? AppliedCartPromotion { get; set; }

    /// <summary>
    /// Результат застосування купона/коду
    /// </summary>
    public ApplyCouponResult CouponResult { get; set; }

    /// <summary>
    /// Додаткова інформація або повідомлення для фронтенду
    /// </summary>
    public string? Message { get; set; }
}

public enum ApplyCouponResult
{
    None,               // Код не вводився
    Applied,            // Код застосований успішно
    Invalid,            // Код не знайдено або неактивний
    NotEligible,        // Код дійсний, але користувач не може його застосувати (наприклад, персональний код для іншого користувача)
    BetterPromotionExists, // Код дійсний, але застосована інша, більш вигідна акція
    UsageLimitExceeded  // Вичерпано глобальний ліміт MaxUsages або персональний ліміт призначення
}