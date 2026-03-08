using LeveLEO.Features.ShoppingCarts.DTO;

namespace LeveLEO.Features.Orders.DTO;

public class CreateOrderResultDto
{
    public Guid OrderId { get; set; }
    public string? Payload { get; set; } = null!;
    public ShoppingCartDto? ShoppingCart { get; set; }
    public string? Message { get; set; }
}