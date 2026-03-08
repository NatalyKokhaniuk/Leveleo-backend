using LeveLEO.Features.Products.DTO;

namespace LeveLEO.Features.Orders.DTO;

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountedUnitPrice { get; set; }
    public decimal TotalOriginalPrice => Quantity * UnitPrice;
    public decimal TotalDiscountedPrice => Quantity * DiscountedUnitPrice;
}