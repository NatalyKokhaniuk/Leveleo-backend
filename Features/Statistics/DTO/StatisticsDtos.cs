namespace LeveLEO.Features.Statistics.DTO;

/// <summary>
/// Статистика продажів в розрізі місяців
/// </summary>
public class MonthlySalesReportDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = null!;
    public int OrdersCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
}

/// <summary>
/// Статистика продажів по днях
/// </summary>
public class DailySalesReportDto
{
    public DateOnly Date { get; set; }
    public int OrdersCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
}

/// <summary>
/// Статистика по окремому продукту
/// </summary>
public class ProductSalesStatsDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string ProductSlug { get; set; } = null!;
    public int UnitsSold { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AveragePrice { get; set; }
    public int CurrentStock { get; set; }
}

/// <summary>
/// Динаміка залишків по продуктах
/// </summary>
public class ProductStockHistoryDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int CurrentStock { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableStock { get; set; }
    public bool IsLowStock { get; set; }
    public int LowStockThreshold { get; set; } = 5;
}

/// <summary>
/// Статистика по промоакціях
/// </summary>
public class PromotionStatsDto
{
    public Guid PromotionId { get; set; }
    public string PromotionName { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public int OrdersWithPromotion { get; set; }
    public decimal TotalDiscountGiven { get; set; }
    public decimal TotalRevenueWithPromotion { get; set; }
    public int UniqueCustomers { get; set; }
}

/// <summary>
/// Загальна статистика дашборду
/// </summary>
public class DashboardStatsDto
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TodayOrders { get; set; }
    public decimal TodayRevenue { get; set; }
    public int PendingOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int ShippedOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public int PendingReviews { get; set; }
}

/// <summary>
/// Фільтри для статистики
/// </summary>
public class SalesReportFilterDto
{
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
}
