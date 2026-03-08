using LeveLEO.Features.Orders.DTO;
using LeveLEO.Features.Orders.Models;
using LeveLEO.Features.Orders.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LeveLEO.Features.Orders.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Всі ендпоінти вимагають авторизації
public class OrdersController(IOrderService orderService) : ControllerBase
{
    #region GET - Order Retrieval

    /// <summary>
    /// Отримати замовлення за ID
    /// Доступно: власник замовлення або адміністратори
    /// </summary>
    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<OrderDetailDto>> GetById(Guid orderId)
    {
        var order = await orderService.GetByIdAsync(orderId);

        // Перевірка доступу: тільки власник або адмін
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("Moderator");

        if (order.UserId != userId && !isAdmin)
        {
            return Forbid();
        }

        return Ok(order);
    }

    /// <summary>
    /// Отримати замовлення за номером
    /// Доступно: власник замовлення або адміністратори
    /// </summary>
    [HttpGet("number/{orderNumber}")]
    public async Task<ActionResult<OrderDetailDto>> GetByNumber(string orderNumber)
    {
        var order = await orderService.GetByNumberAsync(orderNumber);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("Moderator");

        if (order.UserId != userId && !isAdmin)
        {
            return Forbid();
        }

        return Ok(order);
    }

    /// <summary>
    /// Отримати всі замовлення поточного користувача
    /// </summary>
    [HttpGet("my-orders")]
    public async Task<ActionResult<List<OrderListItemDto>>> GetMyOrders(
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var orders = await orderService.GetByUserIdAsync(userId, startDate, endDate);
        return Ok(orders);
    }

    /// <summary>
    /// Отримати всі замовлення конкретного користувача
    /// Доступно: тільки адміністратори
    /// </summary>
    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<List<OrderListItemDto>>> GetUserOrders(
        string userId,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null)
    {
        var orders = await orderService.GetByUserIdAsync(userId, startDate, endDate);
        return Ok(orders);
    }

    #endregion GET - Order Retrieval

    #region POST - Order Creation

    /// <summary>
    /// Створити нове замовлення з кошика
    /// Повертає payload для оплати через LiqPay
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CreateOrderResultDto>> CreateOrder([FromBody] OrderCreateDto orderCreateDto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Отримуємо базовий URL сервера для callback
        var serverUrl = $"{Request.Scheme}://{Request.Host}";

        var result = await orderService.CreateOrderFromCartAsync(userId, orderCreateDto, serverUrl);

        // Якщо кошик змінився, повертаємо 409 Conflict з оновленим кошиком
        if (result.ShoppingCart != null)
        {
            return Conflict(result);
        }

        return CreatedAtAction(nameof(GetById), new { orderId = result.OrderId }, result);
    }

    #endregion POST - Order Creation

    #region PUT - Order Update

    /// <summary>
    /// Оновити замовлення (статус, адреса)
    /// Доступно: адміністратори для будь-яких полів, користувачі - тільки адреса для Pending замовлень
    /// </summary>
    [HttpPut("{orderId:guid}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<OrderDetailDto>> Update(Guid orderId, [FromBody] OrderUpdateDto orderUpdateDto)
    {
        var result = await orderService.UpdateAsync(orderId, orderUpdateDto);
        return Ok(result);
    }

    #endregion PUT - Order Update

    #region Admin Only

    /// <summary>
    /// Скасувати замовлення
    /// Доступно: тільки адміністратори
    /// </summary>
    [HttpPost("{orderId:guid}/cancel")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<OrderDetailDto>> CancelOrder(Guid orderId)
    {
        var updateDto = new OrderUpdateDto
        {
            Status = OrderStatus.Cancelled
        };

        var result = await orderService.UpdateAsync(orderId, updateDto);
        return Ok(result);
    }

    #endregion Admin Only
}