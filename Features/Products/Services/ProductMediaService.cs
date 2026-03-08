using LeveLEO.Data;
using LeveLEO.Features.Products.Models;
using LeveLEO.Infrastructure.Media.Services;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;

namespace LeveLEO.Features.Products.Services;

public class ProductMediaService(AppDbContext db, IMediaService mediaService) : IProductMediaService
{
    #region CRUD

    public async Task<ProductImage> AddImageAsync(Guid productId, string imageKey, int? sortOrder = null)
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
        return image;
    }

    public async Task<ProductVideo> AddVideoAsync(Guid productId, string videoKey, int? sortOrder = null)
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
        return video;
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
    public async Task<List<ProductImage>> GetImagesAsync(Guid productId)
    {
        return await db.ProductImages
            .AsNoTracking()
            .Where(i => i.ProductId == productId)
            .OrderBy(i => i.SortOrder)
            .ToListAsync();
    }

    public async Task<List<ProductVideo>> GetVideosAsync(Guid productId)
    {
        return await db.ProductVideos
            .AsNoTracking()
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.SortOrder)
            .ToListAsync();
    }

    #endregion CRUD
}