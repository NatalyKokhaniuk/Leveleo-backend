using LeveLEO.Infrastructure.Common;

namespace LeveLEO.Features.Categories.DTO;

public class UpdateCategoryDto
{
    public Optional<string> Name { get; set; }

    public Optional<string?> Description { get; set; }

    public Optional<Guid?> ParentId { get; set; }

    public Optional<bool> IsActive { get; set; }
    public Optional<string?> ImageKey { get; set; }
}