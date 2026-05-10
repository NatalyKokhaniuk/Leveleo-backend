using LeveLEO.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LeveLEO.Features.Config.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController(IOptions<CheckoutClientHintsOptions> checkoutClientHints) : ControllerBase
{
    /// <summary>
    /// Рекомендовані для фронтенда параметри чекауту (наприклад HTTP timeout для створення замовлення).
    /// Публичний — можна викликати при старті SPA.
    /// </summary>
    [HttpGet("checkout-client-hints")]
    [AllowAnonymous]
    public ActionResult<CheckoutClientHintsResponse> GetCheckoutClientHints()
    {
        var o = checkoutClientHints.Value;
        return Ok(new CheckoutClientHintsResponse(o.OrderCreationTimeoutSeconds));
    }
}

public record CheckoutClientHintsResponse(int OrderCreationTimeoutSeconds);
