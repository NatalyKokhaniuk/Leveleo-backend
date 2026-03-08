using Microsoft.EntityFrameworkCore;

namespace LeveLEO.Infrastructure.SlugGenerator;

public class SlugGenerator : ISlugGenerator
{
    private const int MaxAttempts = 1000;

    public async Task<string> GenerateUniqueSlugAsync<T>(
        IQueryable<T> query,
        string name,
        Func<T, string> slugSelector)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(name))
            name = "item";

        // 1. Базовий slug через SlugService
        var baseSlug = SlugService.ToSlug(name);
        var slug = baseSlug;
        int counter = 1;

        // 2. Перевірка унікальності
        while (await query.AnyAsync(x => slugSelector(x) == slug))
        {
            counter++;
            if (counter > MaxAttempts)
                throw new InvalidOperationException("Не вдалося згенерувати унікальний slug");

            slug = $"{baseSlug}-{counter}";
        }

        return slug;
    }
}