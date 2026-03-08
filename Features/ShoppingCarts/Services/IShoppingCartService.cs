using LeveLEO.Features.ShoppingCarts.DTO;

namespace LeveLEO.Features.ShoppingCarts.Services;

public interface IShoppingCartService
{
    /// <summary>
    /// Отримати повністю перерахований кошик користувача.
    /// НЕ кидає помилок через відсутність товару.
    /// Автоматично:
    /// - видаляє недоступні товари
    /// - зменшує кількість до доступної
    /// - застосовує найкращі акції
    /// - повертає список видалених та скоригованих айтемів
    /// </summary>
    Task<ShoppingCartDto> GetCalculatedCartAsync(string userId);

    /// <summary>
    /// Додати товар до кошика.
    /// Кидає ApiException якщо товар не існує або недостатньо доступної кількості.
    /// </summary>
    Task<ShoppingCartItemDto> AddItemAsync(string userId, Guid productId, int quantity);

    /// <summary>
    /// Збільшити кількість товару.
    /// Кидає ApiException якщо недостатньо доступної кількості.
    /// </summary>
    Task<ShoppingCartItemDto> IncreaseQuantityAsync(string userId, Guid productId, int amount = 1);

    /// <summary>
    /// Зменшити кількість товару.
    /// Якщо стає 0 — товар видаляється.
    /// </summary>
    Task<ShoppingCartItemDto?> DecreaseQuantityAsync(string userId, Guid productId, int amount = 1);

    /// <summary>
    /// Повністю видалити товар з кошика.
    /// </summary>
    Task RemoveItemAsync(string userId, Guid productId);

    /// <summary>
    /// Застосувати купон до кошика.
    /// Купон перевіряється через PromotionService.
    /// </summary>
    Task<ShoppingCartDto> ApplyCouponAsync(string userId, string couponCode);

    /// <summary>
    /// Видалити купон з кошика.
    /// </summary>
    Task<ShoppingCartDto> RemoveCouponAsync(string userId);

    /// <summary>
    /// Повністю очистити кошик.
    /// Видаляє всі айтеми та купон.
    /// Повертає мінімалістичний результат.
    /// </summary>
    Task<CartClearResultDto> ClearCartAsync(string userId);
}