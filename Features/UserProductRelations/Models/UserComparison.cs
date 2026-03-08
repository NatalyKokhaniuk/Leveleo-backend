using LeveLEO.Features.Identity.Models;
using LeveLEO.Features.Products.Models;
using LeveLEO.Models;

namespace LeveLEO.Features.UserProductRelations.Models;

public class UserComparison : ITimestamped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public string UserId { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Product Product { get; set; } = null!;
}