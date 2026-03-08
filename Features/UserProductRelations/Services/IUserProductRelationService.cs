using LeveLEO.Features.Products.DTO;
using LeveLEO.Features.UserProductRelations.DTO;

namespace LeveLEO.Features.UserProductRelations.Services;

public interface IUserProductRelationService
{
    Task<ProductRelationResultDto> AddToFavoritesAsync(Guid productId, string userId);

    Task<ProductRelationResultDto> RemoveFromFavoritesAsync(Guid productId, string userId);

    Task<ProductRelationResultDto> AddToComparisonAsync(Guid productId, string userId);

    Task<ProductRelationResultDto> RemoveFromComparisonAsync(Guid productId, string userId);

    Task<List<ProductResponseDto>> GetFavoritesByUserIdAsync(string userId);

    Task<List<ProductResponseDto>> GetComparisonByUserIdAsync(string userId);
}