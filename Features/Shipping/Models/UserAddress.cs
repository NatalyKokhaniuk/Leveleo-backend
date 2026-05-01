using LeveLEO.Features.Identity.Models;

namespace LeveLEO.Features.Shipping.Models;

public class UserAddress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = null!;
    public Guid AddressId { get; set; }

    /// <summary>Вибрана / «улюблена» адреса (до одної на користувача). Потрібна колонка IsDefault у БД.</summary>
    public bool IsDefault { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Address Address { get; set; } = null!;
}