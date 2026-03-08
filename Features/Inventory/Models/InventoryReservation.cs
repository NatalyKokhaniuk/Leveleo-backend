using LeveLEO.Features.Products.Models;
using LeveLEO.Models;

namespace LeveLEO.Features.Inventory.Models;

public class InventoryReservation : ITimestamped
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid OrderId { get; set; } // для якого ордеру зарезервовано
    public int Quantity { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Термін дії резерву, щоб звільняти невикористаний резерв
    public DateTimeOffset? ExpiresAt { get; set; }
}