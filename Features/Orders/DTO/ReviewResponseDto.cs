using LeveLEO.Features.Products.DTO;

namespace LeveLEO.Features.Orders.DTO;

public class ReviewResponseDto
{
    public Guid Id { get; set; }
    public Guid OrderItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;

    /// <summary>Посилання на сторінку товару, якщо рядок є в БД.</summary>
    public string? ProductSlug { get; set; }

    public string? ProductMainImageKey { get; set; }

    public bool ProductExistsInCatalog { get; set; }

    /// <summary>Має сенс лише коли ProductExistsInCatalog=true.</summary>
    public bool ProductIsActive { get; set; }

    public ProductCatalogDisplayState ProductCatalogDisplayState { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<ReviewPhotoDto> Photos { get; set; } = [];
    public List<ReviewVideoDto> Videos { get; set; } = [];
}