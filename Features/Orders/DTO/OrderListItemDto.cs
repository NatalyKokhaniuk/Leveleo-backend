using LeveLEO.Features.Orders.Models;
using LeveLEO.Models;

namespace LeveLEO.Features.Orders.DTO;

public class OrderListItemDto : ITimestamped
{
    public Guid Id { get; set; }
    public string Number { get; set; } = null!;
    public OrderStatus Status { get; set; }
    public decimal TotalPayable { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public string? AddressSummary { get; set; }
}