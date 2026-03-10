using LeveLEO.Features.Orders.Models;

namespace LeveLEO.Features.Orders.DTO;

public class AdminOrderFilterDto
{
    /// <summary>
    /// Фільтр по статусу замовлення
    /// </summary>
    public OrderStatus? Status { get; set; }

    /// <summary>
    /// Дата початку періоду
    /// </summary>
    public DateTimeOffset? StartDate { get; set; }

    /// <summary>
    /// Дата кінця періоду
    /// </summary>
    public DateTimeOffset? EndDate { get; set; }

    /// <summary>
    /// Сортування: CreatedAt, TotalPayable, Status
    /// </summary>
    public string SortBy { get; set; } = "CreatedAt";

    /// <summary>
    /// Напрямок сортування: asc або desc
    /// </summary>
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// Сторінка
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Розмір сторінки
    /// </summary>
    public int PageSize { get; set; } = 20;
}
