using LeveLEO.Data;
using LeveLEO.Features.Orders.Models;
using LeveLEO.Features.Statistics.DTO;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace LeveLEO.Features.Statistics.Services;

public class StatisticsService(AppDbContext db) : IStatisticsService
{
    public async Task<List<MonthlySalesReportDto>> GetMonthlySalesAsync(int year)
    {
        var startDate = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = startDate.AddYears(1);

        var monthlyData = await db.Orders
            .Where(o => o.Status == OrderStatus.Completed && o.CreatedAt >= startDate && o.CreatedAt < endDate)
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                OrdersCount = g.Count(),
                TotalRevenue = g.Sum(o => o.TotalPayable)
            })
            .ToListAsync();

        var culture = new CultureInfo("uk-UA");

        return monthlyData.Select(m => new MonthlySalesReportDto
        {
            Year = m.Year,
            Month = m.Month,
            MonthName = culture.DateTimeFormat.GetMonthName(m.Month),
            OrdersCount = m.OrdersCount,
            TotalRevenue = m.TotalRevenue,
            AverageOrderValue = m.OrdersCount > 0 ? m.TotalRevenue / m.OrdersCount : 0
        })
        .OrderBy(m => m.Month)
        .ToList();
    }

    public async Task<List<DailySalesReportDto>> GetDailySalesAsync(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var dailyData = await db.Orders
            .Where(o => o.Status == OrderStatus.Completed && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month, o.CreatedAt.Day })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Day = g.Key.Day,
                OrdersCount = g.Count(),
                TotalRevenue = g.Sum(o => o.TotalPayable)
            })
            .ToListAsync();

        return dailyData.Select(d => new DailySalesReportDto
        {
            Date = new DateOnly(d.Year, d.Month, d.Day),
            OrdersCount = d.OrdersCount,
            TotalRevenue = d.TotalRevenue,
            AverageOrderValue = d.OrdersCount > 0 ? d.TotalRevenue / d.OrdersCount : 0
        })
        .OrderBy(d => d.Date)
        .ToList();
    }

    public async Task<List<ProductSalesStatsDto>> GetTopSellingProductsAsync(SalesReportFilterDto filter, int topN = 10)
    {
        var query = db.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.Product)
            .Where(oi => oi.Order.Status == OrderStatus.Completed);

        if (filter.StartDate.HasValue)
            query = query.Where(oi => oi.Order.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(oi => oi.Order.CreatedAt <= filter.EndDate.Value);

        if (filter.CategoryId.HasValue)
            query = query.Where(oi => oi.Product.CategoryId == filter.CategoryId.Value);

        if (filter.BrandId.HasValue)
            query = query.Where(oi => oi.Product.BrandId == filter.BrandId.Value);

        var productStats = await query
            .GroupBy(oi => new
            {
                oi.ProductId,
                oi.Product.Name,
                oi.Product.Slug,
                oi.Product.StockQuantity
            })
            .Select(g => new ProductSalesStatsDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                ProductSlug = g.Key.Slug,
                UnitsSold = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.DiscountedUnitPrice * oi.Quantity),
                AveragePrice = g.Average(oi => oi.DiscountedUnitPrice),
                CurrentStock = g.Key.StockQuantity
            })
            .OrderByDescending(p => p.UnitsSold)
            .Take(topN)
            .ToListAsync();

        return productStats;
    }

    public async Task<List<ProductStockHistoryDto>> GetProductStockStatusAsync()
    {
        var products = await db.Products
            .Where(p => p.IsActive)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.StockQuantity
            })
            .ToListAsync();

        var reservations = await db.InventoryReservations
            .GroupBy(r => r.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                ReservedQty = g.Sum(r => r.Quantity)
            })
            .ToListAsync();

        var reservationDict = reservations.ToDictionary(r => r.ProductId, r => r.ReservedQty);

        return products.Select(p =>
        {
            var reserved = reservationDict.GetValueOrDefault(p.Id, 0);
            var available = Math.Max(0, p.StockQuantity - reserved);

            return new ProductStockHistoryDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                CurrentStock = p.StockQuantity,
                ReservedQuantity = reserved,
                AvailableStock = available,
                IsLowStock = available <= 5 && available > 0,
                LowStockThreshold = 5
            };
        })
        .OrderBy(p => p.AvailableStock)
        .ToList();
    }

    public async Task<List<PromotionStatsDto>> GetPromotionStatsAsync(bool activeOnly = false)
    {
        var now = DateTimeOffset.UtcNow;

        var promotionsQuery = db.Promotions.AsQueryable();

        if (activeOnly)
        {
            promotionsQuery = promotionsQuery.Where(p =>
                p.IsActive &&
                (p.StartDate <= now) &&
                (p.EndDate >= now)
            );
        }

        var promotions = await promotionsQuery.ToListAsync();

        var stats = new List<PromotionStatsDto>();

        foreach (var promo in promotions)
        {
            // Підраховуємо замовлення з цією промоакцією
            var ordersWithPromo = await db.Orders
                .Where(o => o.Status == OrderStatus.Completed && o.TotalCartDiscount > 0)
                .ToListAsync();

            var uniqueCustomers = ordersWithPromo.Select(o => o.UserId).Distinct().Count();

            stats.Add(new PromotionStatsDto
            {
                PromotionId = promo.Id,
                PromotionName = promo.Name,
                IsActive = promo.IsActive &&
                          (promo.StartDate <= now) &&
                          (promo.EndDate >= now),
                StartDate = promo.StartDate,
                EndDate = promo.EndDate,
                OrdersWithPromotion = ordersWithPromo.Count,
                TotalDiscountGiven = ordersWithPromo.Sum(o => o.TotalCartDiscount + o.TotalProductDiscount),
                TotalRevenueWithPromotion = ordersWithPromo.Sum(o => o.TotalPayable),
                UniqueCustomers = uniqueCustomers
            });
        }

        return stats.OrderByDescending(s => s.TotalRevenueWithPromotion).ToList();
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var today = DateTimeOffset.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var allOrders = await db.Orders.ToListAsync();
        var todayOrders = allOrders.Where(o => o.CreatedAt >= today && o.CreatedAt < tomorrow).ToList();

        var lowStockProducts = await db.Products
            .Where(p => p.IsActive && p.StockQuantity > 0 && p.StockQuantity <= 5)
            .CountAsync();

        var outOfStockProducts = await db.Products
            .Where(p => p.IsActive && p.StockQuantity == 0)
            .CountAsync();

        var pendingReviews = await db.OrderItemReviews
            .Where(r => !r.IsApproved)
            .CountAsync();

        return new DashboardStatsDto
        {
            TotalOrders = allOrders.Count,
            TotalRevenue = allOrders.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.TotalPayable),
            TodayOrders = todayOrders.Count,
            TodayRevenue = todayOrders.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.TotalPayable),
            PendingOrders = allOrders.Count(o => o.Status == OrderStatus.Pending),
            ProcessingOrders = allOrders.Count(o => o.Status == OrderStatus.Processing),
            ShippedOrders = allOrders.Count(o => o.Status == OrderStatus.Shipped),
            CompletedOrders = allOrders.Count(o => o.Status == OrderStatus.Completed),
            LowStockProducts = lowStockProducts,
            OutOfStockProducts = outOfStockProducts,
            PendingReviews = pendingReviews
        };
    }
}