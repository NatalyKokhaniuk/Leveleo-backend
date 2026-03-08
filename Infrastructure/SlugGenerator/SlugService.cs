namespace LeveLEO.Infrastructure.SlugGenerator;

using Slugify;

public static class SlugService
{
    private static readonly SlugHelper _slugHelper = new();

    public static string ToSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "item";

        return _slugHelper.GenerateSlug(value);
    }
}