using LeveLEO.Data;
using LeveLEO.Exceptions;
using LeveLEO.Features.AttributeGroups.DTO;
using LeveLEO.Features.AttributeGroups.Models;
using LeveLEO.Infrastructure.SlugGenerator;
using Microsoft.EntityFrameworkCore;

namespace LeveLEO.Features.AttributeGroups.Services;

public class AttributeGroupService(AppDbContext db, ISlugGenerator slugGenerator) : IAttributeGroupService
{
    #region CRUD

    public async Task<AttributeGroupResponseDto> CreateAsync(CreateAttributeGroupDto dto)
    {
        var slug = await slugGenerator.GenerateUniqueSlugAsync(db.AttributeGroups, dto.Name, x => x.Slug);

        var entity = new AttributeGroup
        {
            Name = dto.Name,
            Slug = slug,
            Description = dto.Description,
            Translations = dto.Translations?.Select(t => new AttributeGroupTranslation
            {
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                Description = t.Description
            }).ToList() ?? []
        };

        db.AttributeGroups.Add(entity);
        await db.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task<AttributeGroupResponseDto> GetByIdAsync(Guid groupId)
    {
        var group = await db.AttributeGroups
            .Include(g => g.Translations)
            .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);

        return group == null
            ? throw new ApiException("ATTRIBUTE_GROUP_NOT_FOUND", $"AttributeGroup with Id '{groupId}' not found.", 404)
            : MapToDto(group);
    }

    public async Task<AttributeGroupResponseDto> GetBySlugAsync(string slug)
    {
        var group = await db.AttributeGroups
            .Include(g => g.Translations)
            .FirstOrDefaultAsync(g => g.Slug == slug && !g.IsDeleted);

        return group == null
            ? throw new ApiException("ATTRIBUTE_GROUP_NOT_FOUND", $"AttributeGroup with Slug '{slug}' not found.", 404)
            : MapToDto(group);
    }

    public async Task<List<AttributeGroupResponseDto>> GetAllAsync()
    {
        var groups = await db.AttributeGroups
            .Include(g => g.Translations)
            .Where(g => !g.IsDeleted)
            .ToListAsync();

        return [.. groups.Select(MapToDto)];
    }

    public async Task<List<AttributeGroupResponseDto>> SearchAsync(string query)
    {
        query = query.ToLowerInvariant();

        var groups = await db.AttributeGroups
            .Include(g => g.Translations)
            .Where(g => !g.IsDeleted &&
                        (EF.Functions.ILike(g.Name, $"%{query}%") ||
                         EF.Functions.ILike(g.Description ?? "", $"%{query}%") ||
                         g.Translations.Any(t =>
                             EF.Functions.ILike(t.Name, $"%{query}%") ||
                             EF.Functions.ILike(t.Description ?? "", $"%{query}%")
                         )))
            .ToListAsync();

        return [.. groups.Select(MapToDto)];
    }

    public async Task<AttributeGroupResponseDto> UpdateAsync(Guid groupId, UpdateAttributeGroupDto dto)
    {
        var entity = await db.AttributeGroups
            .Include(g => g.Translations)
            .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted)
            ?? throw new ApiException("ATTRIBUTE_GROUP_NOT_FOUND", $"AttributeGroup with Id '{groupId}' not found.", 404);

        // Name
        if (dto.Name.HasValue && dto.Name.Value != entity.Name)
        {
            entity.Name = dto.Name.Value!;
            entity.Slug = await slugGenerator.GenerateUniqueSlugAsync(db.AttributeGroups, entity.Name, x => x.Slug);
        }

        // Description
        if (dto.Description.HasValue)
            entity.Description = dto.Description.Value;

        if (dto.Translations.HasValue && dto.Translations.Value != null)
        {
            foreach (var tDto in dto.Translations.Value)
            {
                var translation = entity.Translations.FirstOrDefault(t => t.LanguageCode == tDto.LanguageCode);
                if (translation != null)
                {
                    translation.Name = tDto.Name;
                    translation.Description = tDto.Description;
                }
                else
                {
                    //новий переклад
                    entity.Translations.Add(new AttributeGroupTranslation
                    {
                        LanguageCode = tDto.LanguageCode,
                        Name = tDto.Name,
                        Description = tDto.Description
                    });
                }
            }
        }

        await db.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid groupId)
    {
        var group = await db.AttributeGroups
            .Include(g => g.Attributes)
                .ThenInclude(a => a.Translations)
            .Include(g => g.Attributes)
                .ThenInclude(a => a.Values)
            .Include(g => g.Translations)
            .FirstOrDefaultAsync(g => g.Id == groupId) ?? throw new ApiException("ATTRIBUTE_GROUP_NOT_FOUND", $"AttributeGroup with Id '{groupId}' not found.", 404);

        bool hasProducts = group.Attributes.Any(a => a.Values.Count != 0);

        if (hasProducts)
        {
            // Soft delete
            group.IsDeleted = true;
            foreach (var attr in group.Attributes)
            {
                attr.IsDeleted = true;
            }
        }
        else
        {
            // Hard delete
            foreach (var attr in group.Attributes)
                db.ProductAttributeTranslations.RemoveRange(attr.Translations);

            db.ProductAttributes.RemoveRange(group.Attributes);
            db.AttributeGroupTranslations.RemoveRange(group.Translations);
            db.AttributeGroups.Remove(group);
        }

        await db.SaveChangesAsync();
    }

    #endregion CRUD

    #region Translations

    public async Task<AttributeGroupTranslationResponseDto> AddTranslationAsync(
      Guid groupId,
      CreateAttributeGroupTranslationDto dto)
    {
        var group = await db.AttributeGroups
            .Include(g => g.Translations)
            .FirstOrDefaultAsync(g => g.Id == groupId) ?? throw new ApiException(
                "ATTRIBUTE_GROUP_NOT_FOUND",
                $"AttributeGroup with Id '{groupId}' not found.",
                404);

        if (group.Translations.Any(t => t.LanguageCode == dto.LanguageCode))
            throw new ApiException(
                "ATTRIBUTE_GROUP_TRANSLATION_EXISTS",
                $"Translation for language '{dto.LanguageCode}' already exists.",
                400);

        var translation = new AttributeGroupTranslation
        {
            AttributeGroupId = groupId,
            LanguageCode = dto.LanguageCode,
            Name = dto.Name,
            Description = dto.Description
        };

        group.Translations.Add(translation);

        await db.SaveChangesAsync();

        return new AttributeGroupTranslationResponseDto
        {
            Id = translation.Id,
            LanguageCode = translation.LanguageCode,
            Name = translation.Name,
            Description = translation.Description
        };
    }

    public async Task<AttributeGroupTranslationResponseDto> UpdateTranslationAsync(
    Guid groupId,
    CreateAttributeGroupTranslationDto dto)
    {
        var translation = await db.AttributeGroupTranslations
            .FirstOrDefaultAsync(t =>
                t.AttributeGroupId == groupId &&
                t.LanguageCode == dto.LanguageCode) ?? throw new ApiException(
                "ATTRIBUTE_GROUP_TRANSLATION_NOT_FOUND",
                $"Translation for language '{dto.LanguageCode}' not found for group '{groupId}'.",
                404);

        translation.Name = dto.Name;
        translation.Description = dto.Description;

        await db.SaveChangesAsync();

        return new AttributeGroupTranslationResponseDto
        {
            Id = translation.Id,
            LanguageCode = translation.LanguageCode,
            Name = translation.Name,
            Description = translation.Description
        };
    }

    public async Task DeleteTranslationAsync(Guid groupId, string languageCode)
    {
        var translation = await db.AttributeGroupTranslations
            .FirstOrDefaultAsync(t =>
                t.AttributeGroupId == groupId &&
                t.LanguageCode == languageCode) ?? throw new ApiException(
                "ATTRIBUTE_GROUP_TRANSLATION_NOT_FOUND",
                $"Translation for language '{languageCode}' not found for group '{groupId}'.",
                404);

        db.AttributeGroupTranslations.Remove(translation);
        await db.SaveChangesAsync();
    }

    public async Task<AttributeGroupTranslationResponseDto> GetTranslationAsync(
    Guid groupId,
    string languageCode)
    {
        var translation = await db.AttributeGroupTranslations
            .FirstOrDefaultAsync(t =>
                t.AttributeGroupId == groupId &&
                t.LanguageCode == languageCode);

        return translation == null
            ? throw new ApiException(
                "ATTRIBUTE_GROUP_TRANSLATION_NOT_FOUND",
                $"Translation for language '{languageCode}' not found for group '{groupId}'.",
                404)
            : new AttributeGroupTranslationResponseDto
            {
                Id = translation.Id,
                LanguageCode = translation.LanguageCode,
                Name = translation.Name,
                Description = translation.Description
            };
    }

    public async Task<List<AttributeGroupTranslationResponseDto>> GetTranslationsByGroupIdAsync(Guid groupId)
    {
        var translations = await db.AttributeGroupTranslations
            .Where(t => t.AttributeGroupId == groupId)
            .ToListAsync();

        return [.. translations.Select(t => new AttributeGroupTranslationResponseDto
        {
            Id = t.Id,
            LanguageCode = t.LanguageCode,
            Name = t.Name,
            Description = t.Description
        })];
    }

    #endregion Translations

    #region Helpers

    private static AttributeGroupResponseDto MapToDto(AttributeGroup group)
    {
        return new AttributeGroupResponseDto
        {
            Id = group.Id,
            Name = group.Name,
            Slug = group.Slug,
            Description = group.Description,
            Translations = [.. group.Translations.Select(t => new AttributeGroupTranslationResponseDto
            {
                Id = t.Id,
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                Description = t.Description
            })]
        };
    }

    #endregion Helpers
}
