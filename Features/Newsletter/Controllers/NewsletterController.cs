using LeveLEO.Features.Newsletter.DTO;
using LeveLEO.Features.Newsletter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeveLEO.Features.Newsletter.Controllers;

[ApiController]
[Route("api/newsletter")]
public class NewsletterController : ControllerBase
{
    private readonly INewsletterService _newsletterService;
    private readonly ILogger<NewsletterController> _logger;

    public NewsletterController(
        INewsletterService newsletterService,
        ILogger<NewsletterController> logger)
    {
        _newsletterService = newsletterService;
        _logger = logger;
    }

    /// <summary>
    /// Підписатися на розсилку новин (доступно без авторизації)
    /// </summary>
    [HttpPost("subscribe")]
    [AllowAnonymous]
    public async Task<ActionResult<NewsletterSubscriptionResponseDto>> Subscribe([FromBody] SubscribeNewsletterDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _newsletterService.SubscribeAsync(dto, ipAddress);

        return Ok(result);
    }

    /// <summary>
    /// Відписатися від розсилки (через email + токен)
    /// </summary>
    [HttpPost("unsubscribe")]
    [AllowAnonymous]
    public async Task<ActionResult<NewsletterSubscriptionResponseDto>> Unsubscribe([FromBody] UnsubscribeNewsletterDto dto)
    {
        var result = await _newsletterService.UnsubscribeAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Відписатися через посилання з email (тільки токен в query)
    /// </summary>
    [HttpGet("unsubscribe")]
    [AllowAnonymous]
    public async Task<IActionResult> UnsubscribeByToken([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { message = "Токен відписки не вказано" });
        }

        var result = await _newsletterService.UnsubscribeByTokenAsync(token);

        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Відписка від розсилки</title>
    <style>
        body {{ font-family: Arial, sans-serif; background: #f4f4f4; padding: 50px; text-align: center; }}
        .container {{ max-width: 500px; margin: auto; background: white; padding: 40px; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.1); }}
        h1 {{ color: #ef4444; }}
        p {{ font-size: 16px; color: #444; }}
        a {{ color: #2563eb; text-decoration: none; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>😢 {(result.IsSubscribed ? "Помилка" : "Відписка виконана")}</h1>
        <p>{result.Message}</p>
        <p><a href='https://leveleo.com'>← Повернутися на головну</a></p>
    </div>
</body>
</html>";

        return Content(html, "text/html");
    }

    /// <summary>
    /// Перевірити чи підписаний email (доступно без авторизації)
    /// </summary>
    [HttpGet("check/{email}")]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> CheckSubscription(string email)
    {
        var isSubscribed = await _newsletterService.IsSubscribedAsync(email);
        return Ok(new { isSubscribed });
    }

    /// <summary>
    /// Отримати кількість активних підписників (тільки для адмінів)
    /// </summary>
    [HttpGet("stats/subscribers-count")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<int>> GetSubscribersCount()
    {
        var count = await _newsletterService.GetActiveSubscribersCountAsync();
        return Ok(new { count });
    }

    /// <summary>
    /// Відправити розсилку про новий продукт (тільки для адмінів)
    /// </summary>
    [HttpPost("announce/new-product")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AnnounceNewProduct([FromBody] NewProductAnnouncementDto dto)
    {
        await _newsletterService.SendNewProductAnnouncementAsync(dto);
        return Ok(new { message = "Розсилку про новий продукт надіслано!" });
    }

    /// <summary>
    /// Відправити розсилку про нову акцію (тільки для адмінів)
    /// </summary>
    [HttpPost("announce/new-promotion")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AnnounceNewPromotion([FromBody] NewPromotionAnnouncementDto dto)
    {
        await _newsletterService.SendNewPromotionAnnouncementAsync(dto);
        return Ok(new { message = "Розсилку про нову акцію надіслано!" });
    }

    /// <summary>
    /// Відписати користувача по email (тільки для адміна)
    /// </summary>
    [HttpPost("admin/unsubscribe")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminUnsubscribe([FromBody] string email)
    {
        var result = await _newsletterService.AdminUnsubscribeByEmailAsync(email);

        if (!result)
        {
            return NotFound(new { message = "Активну підписку не знайдено" });
        }

        return Ok(new { message = "Користувача успішно відписано" });
    }

    /// <summary>
    /// Отримати список активних підписників з інформацією про акаунти (тільки для адмінів)
    /// </summary>
    [HttpGet("subscribers")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<ActiveSubscriberDto>>> GetActiveSubscribers()
    {
        var subscribers = await _newsletterService.GetActiveSubscribersAsync();
        return Ok(subscribers);
    }
}