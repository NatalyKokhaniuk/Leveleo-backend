using LeveLEO.Data;
using LeveLEO.Features.Products.DTO;
using LeveLEO.Features.Products.Models;
using LeveLEO.Infrastructure.Media.Services;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;

namespace LeveLEO.Features.Products.Services;

public class ProductMediaService(AppDbContext db, IMediaService mediaService) : IProductMediaService
{
    #region CRUD

    public async Task<ProductImageDto> AddImageAsync(Guid productId, string imageKey, int? sortOrder = null)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId))
            throw new ApiException("PRODUCT_NOT_FOUND", $"Product with Id '{productId}' not found.", 404);

        if (sortOrder == null) // null → на кінець
        {
            var lastSortOrder = await db.ProductImages
                .AsNoTracking()
                .Where(i => i.ProductId == productId)
                .MaxAsync(i => (int?)i.SortOrder) ?? -1;
            sortOrder = lastSortOrder + 1;
        }
        else
        {
            // якщо передано число (0 або більше) — вставляємо на позицію sortOrder
            var toShift = await db.ProductImages
                .Where(i => i.ProductId == productId && i.SortOrder >= sortOrder.Value)
                .ToListAsync();
            foreach (var i in toShift)
                i.SortOrder++;
        }

        var image = new ProductImage
        {
            ProductId = productId,
            ImageKey = imageKey,
            SortOrder = sortOrder.Value
        };

        db.ProductImages.Add(image);
        await db.SaveChangesAsync();
        return ToDto(image);
    }

    public async Task<ProductVideoDto> AddVideoAsync(Guid productId, string videoKey, int? sortOrder = null)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId))
            throw new ApiException("PRODUCT_NOT_FOUND",
            $"Product with Id '{productId}' not found.",
            404);
        if (sortOrder == null)
        {
            var lastSortOrder = await db.ProductVideos
                .AsNoTracking()
                .Where(i => i.ProductId == productId)
                .MaxAsync(i => (int?)i.SortOrder) ?? -1;

            sortOrder = lastSortOrder + 1;
        }
        else
        {
            // якщо передано число (0 або більше) — вставляємо на позицію sortOrder
            var toShift = await db.ProductVideos
                .Where(i => i.ProductId == productId && i.SortOrder >= sortOrder.Value)
                .ToListAsync();
            foreach (var i in toShift)
                i.SortOrder++;
        }

        var video = new ProductVideo
        {
            ProductId = productId,
            VideoKey = videoKey,
            SortOrder = sortOrder.Value
        };

        db.ProductVideos.Add(video);
        await db.SaveChangesAsync();
        return ToDto(video);
    }

    public async Task DeleteImageAsync(Guid imageId)
    {
        var image = await db.ProductImages
        .FirstOrDefaultAsync(i => i.Id == imageId)
        ?? throw new ApiException(
        "PRODUCT_IMAGE_NOT_FOUND",
        $"ProductImage with Id '{imageId}' not found.",
        404
    );

        using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            await mediaService.DeleteFileAsync(image.ImageKey);

            db.ProductImages.Remove(image);
            await db.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteVideoAsync(Guid videoId)
    {
        var video = await db.ProductVideos
            .FirstOrDefaultAsync(v => v.Id == videoId)
            ?? throw new ApiException(
                "PRODUCT_VIDEO_NOT_FOUND", $"ProductVideo with Id '{videoId}' not found.",
              404
            );
        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            await mediaService.DeleteFileAsync(video.VideoKey);

            db.ProductVideos.Remove(video);
            await db.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ================== Get ==================
    public async Task<List<ProductImageDto>> GetImagesAsync(Guid productId)
    {
        var images = await db.ProductImages
            .AsNoTracking()
            .Where(i => i.ProductId == productId)
            .OrderBy(i => i.SortOrder)
            .ToListAsync();

        return images.Select(ToDto).ToList();
    }

    public async Task<List<ProductVideoDto>> GetVideosAsync(Guid productId)
    {
        var videos = await db.ProductVideos
            .AsNoTracking()
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.SortOrder)
            .ToListAsync();

        return videos.Select(ToDto).ToList();
    }

    #endregion CRUD

    #region Mapping

    private static ProductImageDto ToDto(ProductImage image) => new()
    {
        Id = image.Id,
        ProductId = image.ProductId,
        ImageKey = image.ImageKey,
        SortOrder = image.SortOrder,
        CreatedAt = image.CreatedAt,
        UpdatedAt = image.UpdatedAt
    };

    private static ProductVideoDto ToDto(ProductVideo video) => new()
    {
        Id = video.Id,
        ProductId = video.ProductId,
        VideoKey = video.VideoKey,
        SortOrder = video.SortOrder,
        CreatedAt = video.CreatedAt,
        UpdatedAt = video.UpdatedAt
    };

    #endregion Mapping
}