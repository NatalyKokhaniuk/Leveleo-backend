namespace LeveLEO.Features.Products.DTO;

public class ProductFilterDto
{
    public Guid? CategoryId { get; set; } // включає також дочірні категорії
    public Guid? BrandId { get; set; }
    public decimal? PriceFrom { get; set; }
    public decimal? PriceTo { get; set; }

    // Атрибути для фільтрації: ключ – AttributeId, значення – список обраних значень
    public List<AttributeFilterValueDto> AttributeFilters { get; set; } = [];

    public bool IncludeInactive { get; set; } = false;

    // Сортування
    public ProductSortBy SortBy { get; set; } = ProductSortBy.PriceAsc;

    // Фільтр за акцією: повертає тільки продукти, що підпадають під конкретну акцію
    public Guid? PromotionId { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public enum ProductSortBy
{
    PriceAsc,
    PriceDesc,
    AverageRatingDesc,
    TotalSoldDesc
}