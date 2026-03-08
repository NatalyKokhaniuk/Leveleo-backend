namespace LeveLEO.Infrastructure.SlugGenerator;

public interface ISlugGenerator
{
    Task<string> GenerateUniqueSlugAsync<T>(IQueryable<T> query, string name, Func<T, string> slugSelector)
        where T : class;
}