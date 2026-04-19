using LeveLEO.Features.ShoppingCarts.DTO;
using System.Text.Json.Serialization;

namespace LeveLEO.Features.Orders.DTO;

public class CreateOrderResultDto
{
    public Guid OrderId { get; set; }

    /// <summary>Base64 LiqPay checkout <c>data</c>; same meaning as <see cref="Payload"/>.</summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }

    /// <summary>Same as <see cref="Data"/> — included for clients that read <c>payload</c>.</summary>
    public string? Payload { get; set; }

    /// <summary>LiqPay checkout <c>signature</c> (base64 SHA-1 of <c>private_key + data + private_key</c> per LiqPay).</summary>
    public string? Signature { get; set; }

    public ShoppingCartDto? ShoppingCart { get; set; }
    public string? Message { get; set; }
}