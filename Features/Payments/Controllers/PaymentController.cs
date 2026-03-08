using LeveLEO.Features.Payments.DTO;
using LeveLEO.Features.Payments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeveLEO.Features.Payments.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController(IPaymentService paymentService) : ControllerBase
{
    /// <summary>
    /// Повертає інформацію про платіж за його Id
    /// Доступний лише авторизованому користувачу
    /// </summary>
    [HttpGet("{paymentId:guid}")]
    [Authorize] // тут можна додати роль, якщо треба, але зазвичай користувачі дивляться свої власні платежі
    public async Task<ActionResult<PaymentResponseDto>> GetPaymentById(Guid paymentId)
    {
        var payment = await paymentService.GetPaymentByIdAsync(paymentId);

        // Якщо потрібно, можна перевірити, що payment.Order.UserId == User.Identity.Name
        return Ok(payment);
    }

    /// <summary>
    /// Endpoint для колбеків від LiqPay
    /// LiqPay надсилає payload + signature
    /// </summary>
    [HttpPost("callback")]
    [AllowAnonymous] // колбеки приходять від LiqPay, авторизації немає
    public async Task<IActionResult> LiqPayCallback([FromForm] string data, [FromForm] string signature)
    {
        var callback = paymentService.VerifyCallback(data, signature);

        await paymentService.HandleLiqPayCallbackAsync(callback);

        // LiqPay зазвичай чекає простий статус 200
        return Ok("Callback received");
    }

    /// <summary>
    /// Скасування платежу (тільки якщо він не пройшов)
    /// </summary>
    [HttpPost("{paymentId:guid}/cancel")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> CancelPayment(Guid paymentId)
    {
        await paymentService.CancelPaymentAsync(paymentId);
        return Ok(new { message = "Payment canceled" });
    }

    /// <summary>
    /// Повернення коштів
    /// </summary>
    [HttpPost("{paymentId:guid}/refund")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> RefundPayment(Guid paymentId, [FromQuery] decimal? amount = null, [FromQuery] string? reason = null)
    {
        await paymentService.RefundPaymentAsync(paymentId, amount, reason);
        return Ok(new { message = "Refund processed" });
    }

    // GetPaymentStatusAsync можливо і не потрібен окремо, бо GetPaymentByIdAsync вже повертає статус
}