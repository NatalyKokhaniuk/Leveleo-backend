using LeveLEO.Data;
using LeveLEO.Features.Brands.DTO;
using LeveLEO.Features.Brands.Models;
using LeveLEO.Infrastructure.SlugGenerator;
using Microsoft.EntityFrameworkCore;

namespace LeveLEO.Features.Brands.Services;

public class BrandService(AppDbContext db, ISlugGenerator slugGenerator) : IBrandService
{
    private readonly AppDbContext _db = db;
    private readonly ISlugGenerator _slugGenerator = slugGenerator;

    #region CRUD Operations

    public async Task<BrandResponseDto> CreateAsync(CreateBrandDto dto)
    {
        var slug = await _slugGenerator.GenerateUniqueSlugAsync(_db.Brands, dto.Name, x => x.Slug);

        var entity = new Brand
        {
            Name = dto.Name,
            Slug = slug,
            Description = dto.Description,
            LogoKey = dto.LogoKey,
            MetaTitle = dto.MetaTitle,
            MetaDescription = dto.MetaDescription,
            Translations = dto.Translations?.Select(t => new BrandTranslation
            {
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                Description = t.Description
            }).ToList() ?? []
        };

        _db.Brands.Add(entity);
        await _db.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task<BrandResponseDto> UpdateAsync(Guid brandId, UpdateBrandDto dto)
    {
        var entity = await _db.Brands
            .Include(b => b.Translations)
            .FirstOrDefaultAsync(b => b.Id == brandId && !b.IsDeleted)
            ?? throw new ApiException("BRAND_NOT_FOUND", $"Brand with Id '{brandId}' not found.", 404);

        // Name + Slug
        if (dto.Name.HasValue && dto.Name.Value != entity.Name)
        {
            entity.Name = dto.Name.Value!;
            entity.Slug = await _slugGenerator.GenerateUniqueSlugAsync(_db.Brands, entity.Name, x => x.Slug);
        }

        // Description
        if (dto.Description.HasValue)
            entity.Description = dto.Description.Value;

        // LogoKey
        if (dto.LogoKey.HasValue)
            entity.LogoKey = dto.LogoKey.Value;

        // MetaTitle
        if (dto.MetaTitle.HasValue)
            entity.MetaTitle = dto.MetaTitle.Value;

        // MetaDescription
        if (dto.MetaDescription.HasValue)
            entity.MetaDescription = dto.MetaDescription.Value;

        await _db.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid brandId)
    {
        var entity = await _db.Brands
            .Include(b => b.Products)
            .Include(b => b.Translations)
            .FirstOrDefaultAsync(b => b.Id == brandId) ?? throw new ApiException("BRAND_NOT_FOUND", $"Brand with Id '{brandId}' not found.", 404);

        bool hasProducts = entity.Products.Count != 0;
        if (hasProducts)
        {
            entity.IsDeleted = true; // soft delete
        }
        else
        {
            _db.BrandTranslations.RemoveRange(entity.Translations);
            _db.Brands.Remove(entity);
        }

        await _db.SaveChangesAsync();
    }

    public async Task<BrandResponseDto> GetByIdAsync(Guid brandId)
    {
        var brand = await _db.Brands
            .AsNoTracking()
            .Include(b => b.Translations)
            .FirstOrDefaultAsync(b => b.Id == brandId && !b.IsDeleted);

        return brand == null ? throw new ApiException("BRAND_NOT_FOUND", $"Brand with Id '{brandId}' not found.", 404) : MapToDto(brand);
    }

    public async Task<BrandResponseDto> GetBySlugAsync(string slug)
    {
        var entity = await _db.Brands
            .AsNoTracking()
            .Include(b => b.Translations)
            .FirstOrDefaultAsync(b => b.Slug == slug && !b.IsDeleted);

        return entity == null ? throw new ApiException("BRAND_NOT_FOUND", $"Brand with slug '{slug}' not found.", 404) : MapToDto(entity);
    }

    public async Task<List<BrandResponseDto>> GetAllAsync()
    {
        var brands = await _db.Brands
            .AsNoTracking()
            .Include(b => b.Translations)
            .Where(b => !b.IsDeleted)
            .ToListAsync();

        return [.. brands.Select(b => MapToDto(b))];
    }

    public async Task<List<BrandResponseDto>> SearchAsync(string query)
    {
        var matched = await _db.Brands
            .AsNoTracking()
            .Include(b => b.Translations)
            .Where(b => !b.IsDeleted &&
                (
                    EF.Functions.ILike(b.Name, $"%{query}%") ||
                    (b.Description != null && EF.Functions.ILike(b.Description, $"%{query}%")) ||
                    b.Translations.Any(t =>
                        EF.Functions.ILike(t.Name, $"%{query}%") ||
                        (t.Description != null && EF.Functions.ILike(t.Description, $"%{query}%"))
                    )
                )
            )
            .ToListAsync();

        return [.. matched.Select(b => MapToDto(b))];
    }

    #endregion CRUD Operations

    #region Translations

    public async Task AddTranslationAsync(Guid brandId, CreateBrandTranslationDto dto)
    {
        var brand = await _db.Brands
            .Include(b => b.Translations)
            .FirstOrDefaultAsync(b => b.Id == brandId && !b.IsDeleted) ?? throw new ApiException("BRAND_NOT_FOUND", "Brand not found.", 404);

        if (brand.Translations.Any(t => t.LanguageCode == dto.LanguageCode))
            throw new ApiException("TRANSLATION_EXISTS", "Translation already exists.", 400);

        brand.Translations.Add(new BrandTranslation
        {
            LanguageCode = dto.LanguageCode,
            Name = dto.Name,
            Description = dto.Description
        });

        await _db.SaveChangesAsync();
    }

    public async Task UpdateTranslationAsync(Guid brandId, CreateBrandTranslationDto dto)
    {
        var brand = await _db.Brands
            .Include(b => b.Translations)
            .FirstOrDefaultAsync(b => b.Id == brandId && !b.IsDeleted) ?? throw new ApiException("BRAND_NOT_FOUND", "Brand not found.", 404);

        var translation = brand.Translations.FirstOrDefault(t => t.LanguageCode == dto.LanguageCode) ?? throw new ApiException("TRANSLATION_NOT_FOUND", "Translation not found.", 404);

        translation.Name = dto.Name;
        translation.Description = dto.Description;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteTranslationAsync(Guid brandId, string languageCode)
    {
        var brand = await _db.Brands
            .Include(b => b.Translations)
            .FirstOrDefaultAsync(b => b.Id == brandId && !b.IsDeleted) ?? throw new ApiException("BRAND_NOT_FOUND", "Brand not found.", 404);

        var translation = brand.Translations.FirstOrDefault(t => t.LanguageCode == languageCode) ?? throw new ApiException("TRANSLATION_NOT_FOUND", "Translation not found.", 404);

        brand.Translations.Remove(translation);
        await _db.SaveChangesAsync();
    }

    public async Task<List<BrandTranslationResponseDto>> GetTranslationsByBrandIdAsync(Guid brandId)
    {
        var brand = await _db.Brands
            .AsNoTracking()
            .Include(b => b.Translations)
            .FirstOrDefaultAsync(b => b.Id == brandId && !b.IsDeleted);

        return brand == null
            ? throw new ApiException("BRAND_NOT_FOUND", $"Brand with Id '{brandId}' not found.", 404)
            : [.. brand.Translations.Select(t => new BrandTranslationResponseDto
        {
            Id = t.Id,
            LanguageCode = t.LanguageCode,
            Name = t.Name,
            Description = t.Description
        })];
    }

    public async Task<BrandTranslationResponseDto> GetTranslationByIdAsync(Guid brandId, string languageCode)
    {
        var brand = await _db.Brands
            .AsNoTracking()
            .Include(b => b.Translations)
            .FirstOrDefaultAsync(b => b.Id == brandId && !b.IsDeleted) ?? throw new ApiException("BRAND_NOT_FOUND", $"Brand with Id '{brandId}' not found.", 404);

        var translation = brand.Translations.FirstOrDefault(t => t.LanguageCode == languageCode);
        return translation == null
            ? throw new ApiException("TRANSLATION_NOT_FOUND", $"Translation '{languageCode}' not found.", 404)
            : new BrandTranslationResponseDto
            {
                Id = translation.Id,
                LanguageCode = translation.LanguageCode,
                Name = translation.Name,
                Description = translation.Description
            };
    }

    #endregion Translations

    #region Mapping

    private static BrandResponseDto MapToDto(Brand b) => new()
    {
        Id = b.Id,
        Name = b.Name,
        Description = b.Description,
        Slug = b.Slug,
        LogoKey = b.LogoKey,
        MetaTitle = b.MetaTitle,
        MetaDescription = b.MetaDescription,
        Translations = [.. b.Translations.Select(t => new BrandTranslationResponseDto
        {
            Id = t.Id,
            LanguageCode = t.LanguageCode,
            Name = t.Name,
            Description = t.Description
        })]
    };

    #endregion Mapping
}