using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace LeveLEO.Infrastructure.SlugGenerator;

public class SlugGenerator : ISlugGenerator
{
    private const int MaxAttempts = 1000;

    /// <summary>
    /// Генерує унікальний slug для сутності
    /// ВИПРАВЛЕНО: використовує Expression замість Func для сумісності з EF Core
    /// </summary>
    public async Task<string> GenerateUniqueSlugAsync<T>(
        IQueryable<T> query,
        string name,
        Expression<Func<T, string>> slugSelector)  // ✅ Expression замість Func
        where T : class
    {
        if (string.IsNullOrWhiteSpace(name))
            name = "item";

        // 1. Базовий slug через SlugService
        var baseSlug = SlugService.ToSlug(name);
        var slug = baseSlug;

        // 2. Завантажуємо всі існуючі slug що починаються з базового
        // Це ефективніше ніж робити багато запитів до БД
        var existingSlugs = await query
            .Select(slugSelector)  // ✅ EF Core може перекласти Expression в SQL
            .Where(s => s.StartsWith(baseSlug))
            .ToListAsync();

        // 3. Перевірка унікальності в пам'яті (не в SQL)
        int counter = 1;
        while (existingSlugs.Contains(slug))
        {
            counter++;
            if (counter > MaxAttempts)
                throw new InvalidOperationException("Не вдалося згенерувати унікальний slug");

            slug = $"{baseSlug}-{counter}";
        }

        return slug;
    }
}