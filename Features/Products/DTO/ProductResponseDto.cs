using LeveLEO.Features.Promotions.DTO;

namespace LeveLEO.Features.Products.DTO;

public class ProductResponseDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? MainImageKey { get; set; }
    public int StockQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public bool IsActive { get; set; }
    public Guid CategoryId { get; set; }
    public Guid BrandId { get; set; }

    public decimal AverageRating { get; set; } // агрегат
    public int RatingCount { get; set; } // агрегат
    public int TotalSold { get; set; } // агрегат

    public ICollection<ProductTranslationResponseDto> Translations { get; set; } = [];

    public decimal? DiscountedPrice { get; set; }
    public AppliedPromotionDto? AppliedPromotion { get; set; }
}