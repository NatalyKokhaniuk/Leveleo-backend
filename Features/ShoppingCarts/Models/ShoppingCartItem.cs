using LeveLEO.Features.Identity.Models;
using LeveLEO.Features.Products.Models;
using LeveLEO.Models;

namespace LeveLEO.Features.ShoppingCarts.Models;

public class ShoppingCartItem : ITimestamped
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CartId { get; set; }
    public ShoppingCart Cart { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal PriceAfterProductPromotion { get; set; }
    public decimal PriceAfterCartPromotion { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}