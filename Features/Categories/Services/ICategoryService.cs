using LeveLEO.Features.Categories.DTO;
using LeveLEO.Features.Products.DTO;

namespace LeveLEO.Features.Categories.Services;

public interface ICategoryService
{
    Task<CategoryResponseDto> CreateAsync(CreateCategoryDto dto);

    Task<CategoryResponseDto> UpdateAsync(Guid categoryId, UpdateCategoryDto dto);

    Task DeleteAsync(Guid categoryId);

    Task<CategoryResponseDto> GetByIdAsync(Guid categoryId);

    Task<CategoryResponseDto> GetBySlugAsync(string slug);

    Task<List<CategoryResponseDto>> GetAllAsync();

    Task<List<CategoryResponseDto>> SearchAsync(string query);

    Task<CategoryBreadcrumbsDto> GetBreadcrumbsAsync(Guid categoryId);

    // -------------------- Translations --------------------
    Task AddTranslationAsync(Guid categoryId, CreateCategoryTranslationDto dto);

    Task UpdateTranslationAsync(Guid categoryId, CreateCategoryTranslationDto dto);

    Task DeleteTranslationAsync(Guid categoryId, string languageCode);

    Task<List<CategoryTranslationResponseDto>> GetTranslationsByCategoryIdAsync(Guid categoryId);

    Task<CategoryTranslationResponseDto> GetTranslationByIdAsync(Guid categoryId, string languageCode);
}