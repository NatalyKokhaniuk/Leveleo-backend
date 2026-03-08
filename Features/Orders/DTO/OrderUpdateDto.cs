using LeveLEO.Features.Orders.Models;
using LeveLEO.Infrastructure.Common;

namespace LeveLEO.Features.Orders.DTO;

public class OrderUpdateDto
{
    public Optional<OrderStatus> Status { get; set; }
    public Optional<Guid> AddressId { get; set; }
}