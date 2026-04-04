using LeveLEO.Data;
using LeveLEO.Features.ProductAttributes.DTO;
using LeveLEO.Features.ProductAttributes.Models;
using LeveLEO.Infrastructure.SlugGenerator;
using Microsoft.EntityFrameworkCore;

namespace LeveLEO.Features.ProductAttributes.Services;

public class ProductAttributeService(AppDbContext db, ISlugGenerator slugGenerator) : IProductAttributeService
{
    private readonly AppDbContext _db = db;
    private readonly ISlugGenerator _slugGenerator = slugGenerator;

    public async Task<ProductAttributeResponseDto> CreateAsync(CreateProductAttributeDto dto)
    {
        var slug = await _slugGenerator.GenerateUniqueSlugAsync(
            _db.ProductAttributes,
            dto.Name,
            x => x.Slug
        );

        var entity = new ProductAttribute
        {
            AttributeGroupId = dto.AttributeGroupId,
            Name = dto.Name,
            Slug = slug,
            Description = dto.Description,
            Type = dto.Type,
            Unit = dto.Unit,
            IsFilterable = dto.IsFilterable,
            IsComparable = dto.IsComparable,
            Translations = dto.Translations?.Select(t => new ProductAttributeTranslation
            {
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                Description = t.Description
            }).ToList() ?? []
        };

        _db.ProductAttributes.Add(entity);
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<ProductAttributeResponseDto> UpdateAsync(Guid attributeId, UpdateProductAttributeDto dto)
    {
        var attr = await _db.ProductAttributes
            .Include(a => a.Translations)
            .FirstOrDefaultAsync(a => a.Id == attributeId && !a.IsDeleted)
            ?? throw new ApiException("ATTRIBUTE_NOT_FOUND", $"Attribute with Id '{attributeId}' not found.", 404);

        //Attribute Group
        if (dto.AttributeGroupId.HasValue && dto.AttributeGroupId.Value != attr.AttributeGroupId)
        {
            var groupExists = await _db.AttributeGroups.AnyAsync(g => g.Id == dto.AttributeGroupId.Value);
            if (!groupExists)
                throw new ApiException("ATTRIBUTE_GROUP_NOT_FOUND", $"Attribute group with Id '{dto.AttributeGroupId.Value}' not found.", 404);
            attr.AttributeGroupId = dto.AttributeGroupId.Value;
        }

        // Name
        if (dto.Name.HasValue && dto.Name.Value != attr.Name)
        {
            attr.Name = dto.Name.Value!;
            attr.Slug = await _slugGenerator.GenerateUniqueSlugAsync(_db.ProductAttributes, attr.Name, x => x.Slug);
        }

        // Description
        if (dto.Description.HasValue)
            attr.Description = dto.Description.Value;

        // Type
        if (dto.Type.HasValue)
            attr.Type = dto.Type.Value;

        // Unit
        if (dto.Unit.HasValue)
            attr.Unit = dto.Unit.Value;

        // IsFilterable
        if (dto.IsFilterable.HasValue)
            attr.IsFilterable = dto.IsFilterable.Value;

        // IsComparable
        if (dto.IsComparable.HasValue)
            attr.IsComparable = dto.IsComparable.Value;

        // Translations
        if (dto.Translations.HasValue && dto.Translations.Value != null)
        {
            foreach (var tDto in dto.Translations.Value)
            {
                var translation = attr.Translations.FirstOrDefault(t => t.LanguageCode == tDto.LanguageCode);
                if (translation != null)
                {
                    translation.Name = tDto.Name;
                    translation.Description = tDto.Description;
                }
                else
                {
                    attr.Translations.Add(new ProductAttributeTranslation
                    {
                        LanguageCode = tDto.LanguageCode,
                        Name = tDto.Name,
                        Description = tDto.Description
                    });
                }
            }
        }

        await _db.SaveChangesAsync();
        return MapToDto(attr);
    }

    public async Task DeleteAsync(Guid attributeId)
    {
        var attr = await _db.ProductAttributes
            .Include(a => a.Translations)
            .Include(a => a.Values)
            .FirstOrDefaultAsync(a => a.Id == attributeId && !a.IsDeleted) ?? throw new ApiException("ATTRIBUTE_NOT_FOUND", $"Attribute with Id '{attributeId}' not found.", 404);

        if (attr.Values.Count != 0)
        {
            // Soft delete
            attr.IsDeleted = true;
        }
        else
        {
            // Hard delete
            _db.ProductAttributeTranslations.RemoveRange(attr.Translations);
            _db.ProductAttributes.Remove(attr);
        }

        await _db.SaveChangesAsync();
    }

    // Тут додаткові методи для GetById, GetBySlug, GetAll, Search
    public async Task<ProductAttributeResponseDto> GetByIdAsync(Guid attributeId)
    {
        var attr = await _db.ProductAttributes
            .AsNoTracking()
            .Include(a => a.Translations)
            .FirstOrDefaultAsync(a => a.Id == attributeId && !a.IsDeleted);

        return attr == null
            ? throw new ApiException("ATTRIBUTE_NOT_FOUND", $"Attribute with Id '{attributeId}' not found.", 404)
            : MapToDto(attr);
    }

    public async Task<ProductAttributeResponseDto> GetBySlugAsync(string slug)
    {
        var attr = await _db.ProductAttributes
            .AsNoTracking()
            .Include(a => a.Translations)
            .FirstOrDefaultAsync(a => a.Slug == slug && !a.IsDeleted);

        return attr == null
            ? throw new ApiException("ATTRIBUTE_NOT_FOUND", $"Attribute with slug '{slug}' not found.", 404)
            : MapToDto(attr);
    }

    public async Task<IEnumerable<ProductAttributeResponseDto>> GetAllAsync()
    {
        return await _db.ProductAttributes
            .AsNoTracking()
            .Include(a => a.Translations)
            .Where(a => !a.IsDeleted)
            .Select(a => MapToDto(a))
            .ToListAsync();
    }

    public async Task<IEnumerable<ProductAttributeResponseDto>> SearchAsync(string query)
    {
        return await _db.ProductAttributes
            .AsNoTracking()
            .Include(a => a.Translations)
            .Where(a => !a.IsDeleted &&
                (
                    EF.Functions.ILike(a.Name, $"%{query}%") ||
                    (a.Description != null && EF.Functions.ILike(a.Description, $"%{query}%")) ||
                    a.Translations.Any(t =>
                        EF.Functions.ILike(t.Name, $"%{query}%") ||
                        (t.Description != null && EF.Functions.ILike(t.Description, $"%{query}%"))
                    )
                )
            )
            .Select(a => MapToDto(a))
            .ToListAsync();
    }

    public async Task<ProductAttributeTranslationResponseDto> AddTranslationAsync(
    Guid attributeId,
    CreateProductAttributeTranslationDto dto)
    {
        var attr = await _db.ProductAttributes
            .Include(a => a.Translations)
            .FirstOrDefaultAsync(a => a.Id == attributeId && !a.IsDeleted) ?? throw new ApiException("ATTRIBUTE_NOT_FOUND", "Attribute not found.", 404);

        if (attr.Translations.Any(t => t.LanguageCode == dto.LanguageCode))
            throw new ApiException("TRANSLATION_EXISTS", "Translation already exists.", 400);

        var translation = new ProductAttributeTranslation
        {
            LanguageCode = dto.LanguageCode,
            Name = dto.Name,
            Description = dto.Description
        };

        attr.Translations.Add(translation);

        await _db.SaveChangesAsync();

        return new ProductAttributeTranslationResponseDto
        {
            Id = translation.Id,
            LanguageCode = translation.LanguageCode,
            Name = translation.Name,
            Description = translation.Description
        };
    }

    public async Task<ProductAttributeTranslationResponseDto> UpdateTranslationAsync(
        Guid attributeId,
        CreateProductAttributeTranslationDto dto)
    {
        var attr = await _db.ProductAttributes
            .Include(a => a.Translations)
            .FirstOrDefaultAsync(a => a.Id == attributeId && !a.IsDeleted) ?? throw new ApiException("ATTRIBUTE_NOT_FOUND", "Attribute not found.", 404);

        var translation = attr.Translations
            .FirstOrDefault(t => t.LanguageCode == dto.LanguageCode) ?? throw new ApiException("TRANSLATION_NOT_FOUND", "Translation not found.", 404);

        translation.Name = dto.Name;
        translation.Description = dto.Description;

        await _db.SaveChangesAsync();

        return new ProductAttributeTranslationResponseDto
        {
            Id = translation.Id,
            LanguageCode = translation.LanguageCode,
            Name = translation.Name,
            Description = translation.Description
        };
    }

    public async Task DeleteTranslationAsync(Guid attributeId, string languageCode)
    {
        var attr = await _db.ProductAttributes
            .Include(a => a.Translations)
            .FirstOrDefaultAsync(a => a.Id == attributeId && !a.IsDeleted) ?? throw new ApiException("ATTRIBUTE_NOT_FOUND", "Attribute not found.", 404);

        var translation = attr.Translations
            .FirstOrDefault(t => t.LanguageCode == languageCode) ?? throw new ApiException("TRANSLATION_NOT_FOUND", "Translation not found.", 404);

        attr.Translations.Remove(translation);

        await _db.SaveChangesAsync();
    }

    public async Task<ProductAttributeTranslationResponseDto> GetTranslationAsync(
    Guid attributeId,
    string languageCode)
    {
        var attr = await _db.ProductAttributes
            .Include(a => a.Translations)
            .FirstOrDefaultAsync(a => a.Id == attributeId && !a.IsDeleted) ?? throw new ApiException("ATTRIBUTE_NOT_FOUND", "Attribute not found.", 404);

        var translation = attr.Translations
            .FirstOrDefault(t => t.LanguageCode == languageCode);

        return translation == null
            ? throw new ApiException("TRANSLATION_NOT_FOUND", "Translation not found.", 404)
            : new ProductAttributeTranslationResponseDto
            {
                Id = translation.Id,
                LanguageCode = translation.LanguageCode,
                Name = translation.Name,
                Description = translation.Description
            };
    }

    public async Task<IEnumerable<ProductAttributeTranslationResponseDto>> GetTranslationsByAttributeIdAsync(Guid attributeId)
    {
        var attr = await _db.ProductAttributes
    .Include(a => a.Translations)
    .FirstOrDefaultAsync(a => a.Id == attributeId && !a.IsDeleted);

        return attr == null
            ? throw new ApiException("ATTRIBUTE_NOT_FOUND", $"Attribute with Id '{attributeId}' not found.", 404)
            : attr.Translations.Select(t => new ProductAttributeTranslationResponseDto
            {
                Id = t.Id,
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                Description = t.Description
            });
    }

    private static ProductAttributeResponseDto MapToDto(ProductAttribute attr) => new()
    {
        Id = attr.Id,
        AttributeGroupId = attr.AttributeGroupId,
        Name = attr.Name,
        Slug = attr.Slug,
        Description = attr.Description,
        Type = attr.Type,
        Unit = attr.Unit,
        IsFilterable = attr.IsFilterable,
        IsComparable = attr.IsComparable,
        Translations = [.. attr.Translations.Select(t => new ProductAttributeTranslationResponseDto
        {
            Id = t.Id,
            LanguageCode = t.LanguageCode,
            Name = t.Name,
            Description = t.Description
        })]
    };

    public async Task<IEnumerable<ProductAttributeResponseDto>> GetByGroupIdAsync(Guid groupId)
    {
        return await _db.ProductAttributes
            .AsNoTracking()
            .Include(a => a.Translations)
            .Where(a => a.AttributeGroupId == groupId && !a.IsDeleted)
            .Select(a => MapToDto(a))
            .ToListAsync();
    }

    public async Task<IEnumerable<ProductAttributeResponseDto>> GetByGroupSlugAsync(string groupSlug)
    {
        var group = await _db.AttributeGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Slug == groupSlug);

        return group == null
            ? throw new ApiException("ATTRIBUTE_GROUP_NOT_FOUND", $"Attribute group with slug '{groupSlug}' not found.", 404)
            : (IEnumerable<ProductAttributeResponseDto>)await _db.ProductAttributes
            .AsNoTracking()
            .Include(a => a.Translations)
            .Where(a => a.AttributeGroupId == group.Id && !a.IsDeleted)
            .Select(a => MapToDto(a))
            .ToListAsync();
    }
}