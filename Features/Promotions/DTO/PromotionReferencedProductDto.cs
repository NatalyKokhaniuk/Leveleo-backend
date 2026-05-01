using LeveLEO.Features.Products.DTO;

namespace LeveLEO.Features.Promotions.DTO;

/// <summary>Товар із умов акції з даними для відображення (включно із «архівними»).</summary>
public class PromotionReferencedProductDto
{
    public Guid ProductId { get; set; }
    public string? Name { get; set; }
    public string? Slug { get; set; }

    public bool ExistsInCatalog { get; set; }

    /// <summary>Якщо ExistsInCatalog — значення колонки IsActive.</summary>
    public bool IsActive { get; set; }

    public ProductCatalogDisplayState CatalogDisplayState { get; set; }
}
