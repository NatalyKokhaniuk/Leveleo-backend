namespace LeveLEO.Features.Categories.DTO;

/// <summary>
/// DTO для breadcrumbs
/// </summary>
public class CategoryBreadcrumbsDto
{
    public List<CategoryResponseDto> Parents { get; set; } = [];
    public List<CategoryResponseDto> Children { get; set; } = [];
}