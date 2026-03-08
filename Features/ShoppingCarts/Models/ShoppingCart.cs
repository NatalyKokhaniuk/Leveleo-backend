using LeveLEO.Features.Identity.Models;
using LeveLEO.Models;

namespace LeveLEO.Features.ShoppingCarts.Models;

public class ShoppingCart : ITimestamped
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;

    public string? CouponCode { get; set; }

    public List<ShoppingCartItem> Items { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}