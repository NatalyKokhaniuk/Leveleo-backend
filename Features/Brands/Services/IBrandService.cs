using LeveLEO.Features.Brands.DTO;

namespace LeveLEO.Features.Brands.Services;

public interface IBrandService
{
    Task<BrandResponseDto> CreateAsync(CreateBrandDto dto);

    Task<BrandResponseDto> UpdateAsync(Guid brandId, UpdateBrandDto dto);

    Task DeleteAsync(Guid brandId);

    Task<BrandResponseDto> GetByIdAsync(Guid brandId);

    Task<BrandResponseDto> GetBySlugAsync(string slug);

    Task<List<BrandResponseDto>> GetAllAsync();

    Task<List<BrandResponseDto>> SearchAsync(string query);

    // Translations
    Task AddTranslationAsync(Guid brandId, CreateBrandTranslationDto dto);

    Task UpdateTranslationAsync(Guid brandId, CreateBrandTranslationDto dto);

    Task DeleteTranslationAsync(Guid brandId, string languageCode);

    Task<List<BrandTranslationResponseDto>> GetTranslationsByBrandIdAsync(Guid brandId);

    Task<BrandTranslationResponseDto> GetTranslationByIdAsync(Guid brandId, string languageCode);
}