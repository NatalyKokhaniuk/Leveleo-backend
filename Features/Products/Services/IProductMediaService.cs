using LeveLEO.Features.Products.DTO;
using LeveLEO.Features.Products.Models;

namespace LeveLEO.Features.Products.Services;

public interface IProductMediaService
{
    Task<ProductImageDto> AddImageAsync(Guid productId, string imageKey, int? sortOrder = null);

    Task<ProductVideoDto> AddVideoAsync(Guid productId, string videoKey, int? sortOrder = null);

    Task DeleteImageAsync(Guid imageId);

    Task DeleteVideoAsync(Guid videoId);

    Task<List<ProductImageDto>> GetImagesAsync(Guid productId);

    Task<List<ProductVideoDto>> GetVideosAsync(Guid productId);
}