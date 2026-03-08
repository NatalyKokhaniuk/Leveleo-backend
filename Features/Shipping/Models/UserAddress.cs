using LeveLEO.Features.Identity.Models;

namespace LeveLEO.Features.Shipping.Models;

public class UserAddress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = null!;
    public Guid AddressId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Address Address { get; set; } = null!;
}