using LeveLEO.Models;

namespace LeveLEO.Features.Categories.Models;

public class CategoryClosure : ITimestamped
{
    public Guid AncestorId { get; set; }
    public Guid DescendantId { get; set; }
    public int Depth { get; set; }
    public Category Ancestor { get; set; } = null!;
    public Category Descendant { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}