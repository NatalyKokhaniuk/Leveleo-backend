namespace LeveLEO.Infrastructure.Caching;

/// <summary>
/// Сервіс для роботи з кешем
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Отримати значення з кешу
    /// </summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Зберегти значення в кеш
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Видалити з кешу
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Видалити по шаблону (наприклад "products:*")
    /// </summary>
    Task RemoveByPatternAsync(string pattern);

    /// <summary>
    /// Перевірити чи існує ключ
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Отримати або встановити (якщо немає в кеші - виконати фабрику і закешувати)
    /// </summary>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
}
