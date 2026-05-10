using LeveLEO.Features.ShoppingCarts.DTO;
using System.Text.Json.Serialization;

namespace LeveLEO.Features.Orders.DTO;

public class CreateOrderResultDto
{
    public Guid OrderId { get; set; }

    /// <summary>Base64 JSON для поля <c>data</c> у form POST на LiqPay Checkout API v3 (<c>https://www.liqpay.ua/api/3/checkout</c>); те саме, що <see cref="Payload"/>.</summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }

    /// <summary>Те саме, що <see cref="Data"/> — для клієнтів із полем <c>payload</c>.</summary>
    public string? Payload { get; set; }

    /// <summary>Поле <c>signature</c>: base64 SHA-1 від <c>private_key + data + private_key</c> (LiqPay).</summary>
    public string? Signature { get; set; }

    public ShoppingCartDto? ShoppingCart { get; set; }
    public string? Message { get; set; }
}