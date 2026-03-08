using LeveLEO.Features.Products.DTO;
using LeveLEO.Features.Products.Models;

namespace LeveLEO.Features.Products.Services;

public interface IProductService

{
    Task<ProductResponseDto> BuildFullDtoAsync(Guid productId);

    Task<List<ProductResponseDto>> BuildFullDtosAsync(IEnumerable<Guid> productIds);

    Task<ProductResponseDto> CreateAsync(CreateProductDto dto);

    Task<ProductResponseDto> UpdateAsync(Guid productId, UpdateProductDto dto);

    Task DeleteAsync(Guid productId); // софт-видалення

    Task<ProductResponseDto> GetByIdAsync(Guid productId);

    Task<ProductResponseDto> GetBySlugAsync(string slug);

    Task<PagedResultDto<ProductResponseDto>> GetAllAsync(ProductFilterDto filter); // пагінація + фільтри + сортування

    Task<PagedResultDto<ProductResponseDto>> SearchAsync(string query, int page = 1, int pageSize = 20); // пошук по назві та опису + по всіх перекладах, якщо знайдеться хоч десь

    Task<ProductTranslationResponseDto> AddTranslationAsync(Guid productId, ProductTranslationDto dto);

    Task<ProductTranslationResponseDto> UpdateTranslationAsync(Guid productId, ProductTranslationDto dto);

    Task DeleteTranslationAsync(Guid productId, string languageCode);
}