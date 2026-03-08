using LeveLEO.Features.ProductAttributeValues.DTO;

namespace LeveLEO.Features.ProductAttributeValues.Services;

public interface IProductAttributeValueService
{
    Task<ProductAttributeValueResponseDto> CreateAsync(Guid productId, CreateProductAttributeValueDto dto);

    Task<ProductAttributeValueResponseDto> UpdateAsync(Guid valueId, UpdateProductAttributeValueDto dto);

    Task DeleteAsync(Guid valueId);

    Task<ProductAttributeValueResponseDto> GetByIdAsync(Guid valueId);

    Task<IEnumerable<ProductAttributeValueResponseDto>> GetByProductIdAsync(Guid productId);

    Task<ProductAttributeValueTranslationResponseDto> AddTranslationAsync(Guid valueId, CreateProductAttributeValueTranslationDto dto);

    Task<ProductAttributeValueTranslationResponseDto> UpdateTranslationAsync(Guid valueId, CreateProductAttributeValueTranslationDto dto);

    Task DeleteTranslationAsync(Guid valueId, string languageCode);

    Task<IEnumerable<ProductAttributeValueTranslationResponseDto>> GetTranslationsByValueIdAsync(Guid valueId);
}