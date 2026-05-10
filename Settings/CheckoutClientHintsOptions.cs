namespace LeveLEO.Settings;

/// <summary>Рекомендації для клієнта при оформленні замовлення (HTTP timeout тощо).</summary>
public class CheckoutClientHintsOptions
{
    public const string SectionName = "Checkout:ClientHints";

    /// <summary>
    /// Рекомендований таймаут HTTP-запиту для <c>POST /api/Orders</c> (секунди).
    /// Створення замовлення може тривати кілька секунд (БД, резерви, лист); типовий клієнтський дефолт 5–10 с може обривати запит.
    /// </summary>
    public int OrderCreationTimeoutSeconds { get; set; } = 90;
}
