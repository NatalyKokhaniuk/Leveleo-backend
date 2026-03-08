using LeveLEO.Features.ProductAttributes.DTO;

namespace LeveLEO.Features.ProductAttributes.Services;

public interface IProductAttributeService
{
    Task<ProductAttributeResponseDto> CreateAsync(CreateProductAttributeDto dto);

    Task<ProductAttributeResponseDto> UpdateAsync(Guid attributeId, UpdateProductAttributeDto dto);

    Task DeleteAsync(Guid attributeId);

    Task<ProductAttributeResponseDto> GetByIdAsync(Guid attributeId);

    Task<ProductAttributeResponseDto> GetBySlugAsync(string slug);

    Task<IEnumerable<ProductAttributeResponseDto>> GetAllAsync();

    Task<IEnumerable<ProductAttributeResponseDto>> SearchAsync(string query);

    Task<IEnumerable<ProductAttributeResponseDto>> GetByGroupIdAsync(Guid groupId);

    Task<IEnumerable<ProductAttributeResponseDto>> GetByGroupSlugAsync(string groupSlug);

    // TRANSLATIONS — тільки через attributeId + languageCode

    Task<ProductAttributeTranslationResponseDto> AddTranslationAsync(
        Guid attributeId,
        CreateProductAttributeTranslationDto dto);

    Task<ProductAttributeTranslationResponseDto> UpdateTranslationAsync(
        Guid attributeId,
        CreateProductAttributeTranslationDto dto);

    Task DeleteTranslationAsync(Guid attributeId, string languageCode);

    Task<ProductAttributeTranslationResponseDto> GetTranslationAsync(
        Guid attributeId,
        string languageCode);

    Task<IEnumerable<ProductAttributeTranslationResponseDto>>
        GetTranslationsByAttributeIdAsync(Guid attributeId);
}