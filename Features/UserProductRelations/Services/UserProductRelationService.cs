using LeveLEO.Data;
using LeveLEO.Features.Products.DTO;
using LeveLEO.Features.Products.Models;
using LeveLEO.Features.Products.Services;
using LeveLEO.Features.UserProductRelations.DTO;
using LeveLEO.Features.UserProductRelations.Models;
using Microsoft.EntityFrameworkCore;

namespace LeveLEO.Features.UserProductRelations.Services;

public class UserProductRelationService(AppDbContext db, IProductService productService) : IUserProductRelationService
{
    private readonly AppDbContext _db = db;
    private readonly IProductService _productService = productService;

    #region Favorites

    public async Task<ProductRelationResultDto> AddToFavoritesAsync(Guid productId, string userId)
    {
        ValidateInput(productId, userId);

        var exists = await _db.UserFavorites
            .AnyAsync(f => f.ProductId == productId && f.UserId == userId);
        if (!exists)
        {
            _db.UserFavorites.Add(new UserFavorite
            {
                ProductId = productId,
                UserId = userId
            });

            await _db.SaveChangesAsync();
        }

        return new ProductRelationResultDto
        {
            ProductId = productId,
            IsInRelation = true
        };
    }

    public async Task<ProductRelationResultDto> RemoveFromFavoritesAsync(Guid productId, string userId)
    {
        ValidateInput(productId, userId);
        await _db.UserFavorites
        .Where(f => f.ProductId == productId && f.UserId == userId)
        .ExecuteDeleteAsync();
        return new ProductRelationResultDto
        {
            ProductId = productId,
            IsInRelation = false
        };
    }

    public async Task<List<ProductResponseDto>> GetFavoritesByUserIdAsync(string userId)
    {
        var productIds = await _db.UserFavorites
        .Where(f => f.UserId == userId)
        .Select(f => f.ProductId)
        .ToListAsync();

        return await _productService.BuildFullDtosAsync(productIds);
    }

    #endregion Favorites

    #region Comparison

    public async Task<ProductRelationResultDto> AddToComparisonAsync(Guid productId, string userId)
    {
        ValidateInput(productId, userId);

        var exists = await _db.UserComparisons
            .AnyAsync(c => c.ProductId == productId && c.UserId == userId);

        if (!exists)
        {
            _db.UserComparisons.Add(new UserComparison
            {
                ProductId = productId,
                UserId = userId
            });

            await _db.SaveChangesAsync();
        }
        return new ProductRelationResultDto
        {
            ProductId = productId,
            IsInRelation = true
        };
    }

    public async Task<ProductRelationResultDto> RemoveFromComparisonAsync(Guid productId, string userId)
    {
        ValidateInput(productId, userId);

        await _db.UserComparisons
            .Where(c => c.ProductId == productId && c.UserId == userId)
            .ExecuteDeleteAsync();

        return new ProductRelationResultDto
        {
            ProductId = productId,
            IsInRelation = false
        };
    }

    public async Task<List<ProductResponseDto>> GetComparisonByUserIdAsync(string userId)
    {
        var productIds = await _db.UserComparisons
        .Where(f => f.UserId == userId)
        .Select(f => f.ProductId)
        .ToListAsync();

        return await _productService.BuildFullDtosAsync(productIds);
    }

    #endregion Comparison

    private static void ValidateInput(Guid productId, string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ApiException("USER_ID_REQUIRED", "User ID is required", 400);

        if (productId == Guid.Empty)
            throw new ApiException("PRODUCT_ID_REQUIRED", "Product ID is required", 400);
    }
}