using System.Linq.Expressions;

namespace LeveLEO.Infrastructure.SlugGenerator;

public interface ISlugGenerator
{
    Task<string> GenerateUniqueSlugAsync<T>(IQueryable<T> query, string name, Expression<Func<T, string>> slugSelector)
        where T : class;
}