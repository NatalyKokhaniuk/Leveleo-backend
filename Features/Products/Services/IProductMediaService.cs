using LeveLEO.Features.Products.Models;

namespace LeveLEO.Features.Products.Services;

public interface IProductMediaService
{
    Task<ProductImage> AddImageAsync(Guid productId, string imageKey, int? sortOrder = null);

    Task<ProductVideo> AddVideoAsync(Guid productId, string videoKey, int? sortOrder = null);

    Task DeleteImageAsync(Guid imageId);

    Task DeleteVideoAsync(Guid videoId);

    Task<List<ProductImage>> GetImagesAsync(Guid productId);

    Task<List<ProductVideo>> GetVideosAsync(Guid productId);
}