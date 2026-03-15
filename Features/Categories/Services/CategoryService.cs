using LeveLEO.Data;
using LeveLEO.Features.Categories.DTO;
using LeveLEO.Features.Categories.Models;
using LeveLEO.Infrastructure.SlugGenerator;
using Microsoft.EntityFrameworkCore;

namespace LeveLEO.Features.Categories.Services;

public class CategoryService(AppDbContext db, ISlugGenerator slugGenerator) : ICategoryService
{
    #region CRUD Categories

    public async Task<CategoryResponseDto> CreateAsync(CreateCategoryDto dto)
    {
        var slug = await slugGenerator.GenerateUniqueSlugAsync(db.Categories, dto.Name, x => x.Slug);

        var category = new Category
        {
            Name = dto.Name,
            Slug = slug,
            Description = dto.Description,
            ParentId = dto.ParentId,
            IsActive = dto.IsActive,
            Translations = dto.Translations?.Select(t => new CategoryTranslation
            {
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                Description = t.Description
            }).ToList() ?? []
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        await UpdateClosureForNewCategory(category);
        var paths = await BuildCategoryPathsAsync([category.Id]);

        return MapToDto(category, paths);
    }

    public async Task<CategoryResponseDto> UpdateAsync(Guid categoryId, UpdateCategoryDto dto)
    {
        var category = await db.Categories
            .Include(c => c.Translations)
            .FirstOrDefaultAsync(c => c.Id == categoryId && !c.IsDeleted)
            ?? throw new ApiException("CATEGORY_NOT_FOUND",
                $"Category with Id '{categoryId}' not found.", 404);

        var oldParentId = category.ParentId;

        if (dto.Name.HasValue && dto.Name.Value != category.Name)
        {
            category.Name = dto.Name.Value!;
            category.Slug = await slugGenerator.GenerateUniqueSlugAsync(
                db.Categories,
                dto.Name.Value!,
                x => x.Slug);
        }

        if (dto.Description.HasValue)
        {
            category.Description = dto.Description.Value;
        }

        if (dto.ParentId.HasValue)
        {
            category.ParentId = dto.ParentId.Value;
        }

        if (dto.IsActive.HasValue)
        {
            category.IsActive = dto.IsActive.Value;
        }

        await db.SaveChangesAsync();

        if (dto.ParentId.HasValue && dto.ParentId.Value != oldParentId)
        {
            await RebuildClosureAsync(category);
        }

        var paths = await BuildCategoryPathsAsync([category.Id]);

        return MapToDto(category, paths);
    }

    public async Task DeleteAsync(Guid categoryId)
    {
        var category = await db.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == categoryId) ?? throw new ApiException("CATEGORY_NOT_FOUND", $"Category with Id '{categoryId}' not found.", 404);

        var descendantIds = await db.CategoryClosures
            .Where(c => c.AncestorId == categoryId)
            .Select(c => c.DescendantId)
            .ToListAsync();

        bool hasProducts = await db.Products.AnyAsync(p => descendantIds.Contains(p.CategoryId));

        if (hasProducts)
        {
            var categoriesToSoftDelete = await db.Categories
                .Where(c => descendantIds.Contains(c.Id))
                .ToListAsync();
            foreach (var c in categoriesToSoftDelete)
                c.IsDeleted = true;
        }
        else
        {
            var closuresToDelete = await db.CategoryClosures
                .Where(c => descendantIds.Contains(c.DescendantId))
                .ToListAsync();
            db.CategoryClosures.RemoveRange(closuresToDelete);

            var translationsToDelete = await db.CategoryTranslations
                .Where(t => descendantIds.Contains(t.CategoryId))
                .ToListAsync();
            db.CategoryTranslations.RemoveRange(translationsToDelete);

            var categoriesToDelete = await db.Categories
                .Where(c => descendantIds.Contains(c.Id))
                .ToListAsync();
            db.Categories.RemoveRange(categoriesToDelete);
        }

        await db.SaveChangesAsync();
    }

    public async Task<CategoryResponseDto> GetByIdAsync(Guid categoryId)
    {
        var category = await db.Categories
            .AsNoTracking()
            .Include(c => c.Translations)
            .FirstOrDefaultAsync(c => c.Id == categoryId && !c.IsDeleted) ?? throw new ApiException("CATEGORY_NOT_FOUND", $"Category with Id '{categoryId}' not found.", 404);

        var paths = await BuildCategoryPathsAsync([category.Id]);

        return MapToDto(category, paths);
    }

    public async Task<CategoryResponseDto> GetBySlugAsync(string slug)
    {
        var category = await db.Categories
            .AsNoTracking()
            .Include(c => c.Translations)
            .FirstOrDefaultAsync(c => c.Slug == slug && !c.IsDeleted) ?? throw new ApiException("CATEGORY_NOT_FOUND", $"Category with Slug '{slug}' not found.", 404);

        var paths = await BuildCategoryPathsAsync([category.Id]);

        return MapToDto(category, paths);
    }

    public async Task<List<CategoryResponseDto>> GetAllAsync()
    {
        var categories = await db.Categories
            .AsNoTracking()
        .Include(c => c.Translations)
        .Where(c => !c.IsDeleted)
        .ToListAsync();

        var paths = await BuildCategoryPathsAsync();

        return [.. categories
            .OrderBy(c => paths.GetValueOrDefault(c.Id))
            .Select(c => MapToDto(c, paths))];
    }

    public async Task<List<CategoryResponseDto>> SearchAsync(string query)
    {
        var categories = await db.Categories
        .Include(c => c.Translations)
        .Where(c => !c.IsDeleted &&
                    (c.Name.Contains(query) ||
                     (c.Description != null && c.Description.Contains(query)) ||
                     c.Translations.Any(t => t.Name.Contains(query) ||
                                             (t.Description != null && t.Description.Contains(query)))))
        .AsNoTracking()
        .ToListAsync();

        var paths = await BuildCategoryPathsAsync();

        return [.. categories
            .OrderBy(c => paths.GetValueOrDefault(c.Id))
            .Select(c => MapToDto(c, paths))];
    }

    #endregion CRUD Categories

    #region Translations

    public async Task AddTranslationAsync(Guid categoryId, CreateCategoryTranslationDto dto)
    {
        var category = await db.Categories
            .Include(c => c.Translations)
            .FirstOrDefaultAsync(c => c.Id == categoryId && !c.IsDeleted) ?? throw new ApiException("CATEGORY_NOT_FOUND", $"Category with Id '{categoryId}' not found.", 404);

        if (category.Translations.Any(t => t.LanguageCode == dto.LanguageCode))
            throw new InvalidOperationException($"Translation for language '{dto.LanguageCode}' already exists.");

        category.Translations.Add(new CategoryTranslation
        {
            CategoryId = categoryId,
            LanguageCode = dto.LanguageCode,
            Name = dto.Name,
            Description = dto.Description
        });

        await db.SaveChangesAsync();
    }

    public async Task UpdateTranslationAsync(Guid categoryId, CreateCategoryTranslationDto dto)
    {
        var translation = await db.CategoryTranslations
            .FirstOrDefaultAsync(t => t.CategoryId == categoryId && t.LanguageCode == dto.LanguageCode) ?? throw new ApiException("CATEGORY_TRANSLATION_NOT_FOUND",
                $"Translation for category '{categoryId}' and language '{dto.LanguageCode}' not found.", 404);

        translation.Name = dto.Name;
        translation.Description = dto.Description;

        await db.SaveChangesAsync();
    }

    public async Task DeleteTranslationAsync(Guid categoryId, string languageCode)
    {
        var translation = await db.CategoryTranslations
            .FirstOrDefaultAsync(t => t.CategoryId == categoryId && t.LanguageCode == languageCode) ?? throw new ApiException("CATEGORY_TRANSLATION_NOT_FOUND",
                $"Translation for category '{categoryId}' and language '{languageCode}' not found.", 404);

        db.CategoryTranslations.Remove(translation);
        await db.SaveChangesAsync();
    }

    public async Task<List<CategoryTranslationResponseDto>> GetTranslationsByCategoryIdAsync(Guid categoryId)
    {
        var category = await db.Categories
            .AsNoTracking()
            .Include(c => c.Translations)
            .FirstOrDefaultAsync(c => c.Id == categoryId && !c.IsDeleted);

        return category == null
            ? throw new ApiException("CATEGORY_NOT_FOUND", $"Category with Id '{categoryId}' not found.", 404)
            : [.. category.Translations
            .Select(t => new CategoryTranslationResponseDto
            {
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                Description = t.Description
            })];
    }

    public async Task<CategoryTranslationResponseDto> GetTranslationByIdAsync(Guid categoryId, string languageCode)
    {
        var translation = await db.CategoryTranslations
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.CategoryId == categoryId && t.LanguageCode == languageCode);

        return translation == null
            ? throw new ApiException("CATEGORY_TRANSLATION_NOT_FOUND",
                $"Translation for category '{categoryId}' and language '{languageCode}' not found.", 404)
            : new CategoryTranslationResponseDto
            {
                LanguageCode = translation.LanguageCode,
                Name = translation.Name,
                Description = translation.Description
            };
    }

    #endregion Translations

    #region Breadcrumbs / Hierarchy

    public async Task<CategoryBreadcrumbsDto> GetBreadcrumbsAsync(Guid categoryId)
    {
        var ancestorIds = await db.CategoryClosures
            .AsNoTracking()
            .Where(c => c.DescendantId == categoryId && c.Depth > 0)
            .OrderBy(c => c.Depth)
            .Select(c => c.AncestorId)
            .ToListAsync();

        var parents = await db.Categories
            .AsNoTracking()
            .Where(c => ancestorIds.Contains(c.Id))
            .Include(c => c.Translations)
            .ToListAsync();

        var descendantIds = await db.CategoryClosures
            .AsNoTracking()
            .Where(c => c.AncestorId == categoryId && c.Depth > 0)
            .Select(c => c.DescendantId)
            .ToListAsync();

        var children = await db.Categories
            .AsNoTracking()
            .Where(c => descendantIds.Contains(c.Id))
            .Include(c => c.Translations)
            .ToListAsync();
        var allIds = parents.Select(p => p.Id)
            .Concat(children.Select(c => c.Id))
            .ToList();
        var paths = await BuildCategoryPathsAsync(allIds);
        return new CategoryBreadcrumbsDto
        {
            Parents = [.. parents.Select(p => MapToDto(p, paths))],
            Children = [.. children.Select(c => MapToDto(c, paths))]
        };
    }

    #endregion Breadcrumbs / Hierarchy

    #region Private Helpers (Closure Table)

    private async Task UpdateClosureForNewCategory(Category category)
    {
        db.CategoryClosures.Add(new CategoryClosure
        {
            AncestorId = category.Id,
            DescendantId = category.Id,
            Depth = 0
        });

        if (category.ParentId.HasValue)
        {
            var parentClosures = await db.CategoryClosures
                .Where(c => c.DescendantId == category.ParentId.Value)
                .ToListAsync();

            foreach (var pc in parentClosures)
            {
                db.CategoryClosures.Add(new CategoryClosure
                {
                    AncestorId = pc.AncestorId,
                    DescendantId = category.Id,
                    Depth = pc.Depth + 1
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task RebuildClosureAsync(Category category)
    {
        var descendantIds = await db.CategoryClosures
            .Where(c => c.AncestorId == category.Id)
            .Select(c => c.DescendantId)
            .ToListAsync();

        var closuresToRemove = await db.CategoryClosures
            .Where(c => descendantIds.Contains(c.DescendantId))
            .ToListAsync();
        db.CategoryClosures.RemoveRange(closuresToRemove);
        await db.SaveChangesAsync();

        var categoriesToUpdate = await db.Categories
            .Where(c => descendantIds.Contains(c.Id))
            .ToListAsync();

        foreach (var cat in categoriesToUpdate)
        {
            await UpdateClosureForNewCategory(cat);
        }
    }

    private static CategoryResponseDto MapToDto(Category category,
    Dictionary<Guid, string> paths)
    {
        return new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ParentId = category.ParentId,
            IsActive = category.IsActive,
            FullPath = paths.GetValueOrDefault(category.Id) ?? category.Name,
            Translations = [.. category.Translations.Select(t => new CategoryTranslationResponseDto
            {
                Id = t.Id,
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                Description = t.Description
            })]
        };
    }

    private async Task<Dictionary<Guid, string>> BuildCategoryPathsAsync(IEnumerable<Guid>? categoryIds = null)
    {
        var query = db.CategoryClosures
            .Include(c => c.Ancestor)
            .AsNoTracking();

        if (categoryIds != null)
        {
            query = query.Where(c => categoryIds.Contains(c.DescendantId));
        }

        var closures = await query.ToListAsync();

        return closures
            .GroupBy(c => c.DescendantId)
            .ToDictionary(
                g => g.Key,
                g => string.Join(">",
                    g.OrderByDescending(x => x.Depth)
                     .Select(x => x.Ancestor.Name))
            );
    }

    #endregion Private Helpers (Closure Table)
}
