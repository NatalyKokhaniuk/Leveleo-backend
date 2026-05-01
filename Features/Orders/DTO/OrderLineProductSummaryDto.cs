using LeveLEO.Features.Products.DTO;

namespace LeveLEO.Features.Orders.DTO;

/// <summary>Зріз товару для рядка замовлення та підказок UI («архів», зображення, посилання).</summary>
public class OrderLineProductSummaryDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string? MainImageKey { get; set; }

    /// <summary>Є рядок у таблиці Products.</summary>
    public bool ExistsInCatalog { get; set; }

    /// <summary>Має сенс лише коли ExistsInCatalog; відповідає Products.IsActive.</summary>
    public bool IsActive { get; set; }

    public ProductCatalogDisplayState CatalogDisplayState { get; set; }
}
