using LeveLEO.Features.Statistics.DTO;

namespace LeveLEO.Features.Statistics.Services;

public interface IStatisticsService
{
    /// <summary>
    /// Отримати статистику продажів по місяцях
    /// </summary>
    Task<List<MonthlySalesReportDto>> GetMonthlySalesAsync(int year);

    /// <summary>
    /// Отримати статистику продажів по днях
    /// </summary>
    Task<List<DailySalesReportDto>> GetDailySalesAsync(DateTimeOffset startDate, DateTimeOffset endDate);

    /// <summary>
    /// Отримати топ N продуктів за кількістю продажів
    /// </summary>
    Task<List<ProductSalesStatsDto>> GetTopSellingProductsAsync(SalesReportFilterDto filter, int topN = 10);

    /// <summary>
    /// Отримати динаміку залишків по продуктах
    /// </summary>
    Task<List<ProductStockHistoryDto>> GetProductStockStatusAsync();

    /// <summary>
    /// Отримати статистику по промоакціях
    /// </summary>
    Task<List<PromotionStatsDto>> GetPromotionStatsAsync(bool activeOnly = false);

    /// <summary>
    /// Отримати загальну статистику для дашборду
    /// </summary>
    Task<DashboardStatsDto> GetDashboardStatsAsync();
}
