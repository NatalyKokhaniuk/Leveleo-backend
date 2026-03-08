using LeveLEO.Infrastructure.Translation.Models;

namespace LeveLEO.Features.Categories.Models;

public class CategoryTranslation : ITranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public string LanguageCode { get; set; } = "uk";
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    // Навігація назад до категорії
    public Category Category { get; set; } = null!;
}