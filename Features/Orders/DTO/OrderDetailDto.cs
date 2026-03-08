using LeveLEO.Features.Orders.Models;
using LeveLEO.Features.Payments.DTO;
using LeveLEO.Features.Shipping.DTO;
using LeveLEO.Models;

namespace LeveLEO.Features.Orders.DTO;

public class OrderDetailDto : ITimestamped
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public string Number { get; set; } = null!;
    public OrderStatus Status { get; set; }

    public decimal TotalOriginalPrice { get; set; }
    public decimal TotalProductDiscount { get; set; }
    public decimal TotalCartDiscount { get; set; }
    public decimal TotalPayable { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public AddressResponseDto Address { get; set; } = null!;
    public DeliveryResponseDto? Delivery { get; set; }
    public PaymentResponseDto? Payment { get; set; }

    public List<OrderItemDto> OrderItems { get; set; } = [];
}