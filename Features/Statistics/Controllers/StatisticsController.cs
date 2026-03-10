using LeveLEO.Features.Statistics.DTO;
using LeveLEO.Features.Statistics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeveLEO.Features.Statistics.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class StatisticsController(IStatisticsService statisticsService) : ControllerBase
{
    /// <summary>
    /// Отримати загальну статистику для дашборду
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
    {
        var stats = await statisticsService.GetDashboardStatsAsync();
        return Ok(stats);
    }

    /// <summary>
    /// Отримати статистику продажів по місяцях
    /// </summary>
    [HttpGet("sales/monthly")]
    public async Task<ActionResult<List<MonthlySalesReportDto>>> GetMonthlySales([FromQuery] int year = 0)
    {
        if (year == 0)
            year = DateTime.UtcNow.Year;

        var stats = await statisticsService.GetMonthlySalesAsync(year);
        return Ok(stats);
    }

    /// <summary>
    /// Отримати статистику продажів по днях
    /// </summary>
    [HttpGet("sales/daily")]
    public async Task<ActionResult<List<DailySalesReportDto>>> GetDailySales(
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null)
    {
        startDate ??= DateTimeOffset.UtcNow.AddMonths(-1);
        endDate ??= DateTimeOffset.UtcNow;

        var stats = await statisticsService.GetDailySalesAsync(startDate.Value, endDate.Value);
        return Ok(stats);
    }

    /// <summary>
    /// Отримати топ продуктів за продажами
    /// </summary>
    [HttpGet("products/top-selling")]
    public async Task<ActionResult<List<ProductSalesStatsDto>>> GetTopSellingProducts(
        [FromQuery] SalesReportFilterDto filter,
        [FromQuery] int top = 10)
    {
        var stats = await statisticsService.GetTopSellingProductsAsync(filter, top);
        return Ok(stats);
    }

    /// <summary>
    /// Отримати динаміку залишків по продуктах
    /// </summary>
    [HttpGet("products/stock-status")]
    public async Task<ActionResult<List<ProductStockHistoryDto>>> GetProductStockStatus()
    {
        var stats = await statisticsService.GetProductStockStatusAsync();
        return Ok(stats);
    }

    /// <summary>
    /// Отримати статистику по промоакціях
    /// </summary>
    [HttpGet("promotions")]
    public async Task<ActionResult<List<PromotionStatsDto>>> GetPromotionStats([FromQuery] bool activeOnly = false)
    {
        var stats = await statisticsService.GetPromotionStatsAsync(activeOnly);
        return Ok(stats);
    }
}
