using LeveLEO.Features.Identity.Enums;
using LeveLEO.Features.Orders.Models;
using LeveLEO.Features.Shipping.Models;
using LeveLEO.Features.ShoppingCarts.Models;
using LeveLEO.Features.UserProductRelations.Models;
using LeveLEO.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeveLEO.Features.Identity.Models;

public class ApplicationUser : IdentityUser, ITimestamped
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Language { get; set; } = "uk";
    public string? AvatarKey { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public TwoFactorMethod TwoFactorMethod { get; set; } = TwoFactorMethod.None;
    public bool IsTwoFactorVerified { get; set; }
    public string? TotpSecret { get; set; }
    public string? BackupCodes { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<UserFavorite> Favorites { get; set; } = [];
    public ICollection<UserComparison> Comparisons { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];

    public ICollection<UserAddress> UserAddresses { get; set; } = [];
    public Guid? ShoppingCarttId { get; set; }
    public ShoppingCart? ShoppingCart { get; set; }
}