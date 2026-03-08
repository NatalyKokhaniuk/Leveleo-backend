using LeveLEO.Data;
using LeveLEO.Features.ProductAttributeValues.DTO;
using LeveLEO.Features.ProductAttributeValues.Models;
using Microsoft.EntityFrameworkCore;

namespace LeveLEO.Features.ProductAttributeValues.Services;

public class ProductAttributeValueService(AppDbContext db) : IProductAttributeValueService
{
    #region CRUD

    public async Task<ProductAttributeValueResponseDto> CreateAsync(Guid productId, CreateProductAttributeValueDto dto)
    {
        var pav = new ProductAttributeValue
        {
            ProductId = productId,
            ProductAttributeId = dto.ProductAttributeId,
            StringValue = dto.StringValue,
            DecimalValue = dto.DecimalValue,
            IntValue = dto.IntValue,
            BoolValue = dto.BoolValue,
            Translations = dto.Translations?.Select(t => new ProductAttributeValueTranslation
            {
                LanguageCode = t.LanguageCode,
                Value = t.Value
            }).ToList() ?? []
        };

        db.ProductAttributeValues.Add(pav);
        await db.SaveChangesAsync();

        return MapToDto(pav);
    }

    public async Task<ProductAttributeValueResponseDto> UpdateAsync(Guid valueId, UpdateProductAttributeValueDto dto)
    {
        var pav = await db.ProductAttributeValues
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == valueId) ?? throw new Exception("ProductAttributeValue not found");

        if (dto.ProductAttributeId.HasValue)
            pav.ProductAttributeId = dto.ProductAttributeId.Value;

        if (dto.StringValue.HasValue)
            pav.StringValue = dto.StringValue.Value;

        if (dto.DecimalValue.HasValue)
            pav.DecimalValue = dto.DecimalValue.Value;

        if (dto.IntValue.HasValue)
            pav.IntValue = dto.IntValue.Value;

        if (dto.BoolValue.HasValue)
            pav.BoolValue = dto.BoolValue.Value;

        // Оновлення або додавання перекладів
        if (dto.Translations.HasValue && dto.Translations.Value != null)
        {
            foreach (var tDto in dto.Translations.Value)
            {
                var translation = pav.Translations.FirstOrDefault(t => t.LanguageCode == tDto.LanguageCode);
                if (translation != null)
                {
                    translation.Value = tDto.Value;
                }
                else
                {
                    pav.Translations.Add(new ProductAttributeValueTranslation
                    {
                        LanguageCode = tDto.LanguageCode,
                        Value = tDto.Value
                    });
                }
            }
        }

        await db.SaveChangesAsync();
        return MapToDto(pav);
    }

    public async Task DeleteAsync(Guid valueId)
    {
        var pav = await db.ProductAttributeValues
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == valueId) ?? throw new Exception("ProductAttributeValue not found");

        db.ProductAttributeValueTranslations.RemoveRange(pav.Translations);
        db.ProductAttributeValues.Remove(pav);

        await db.SaveChangesAsync();
    }

    public async Task<ProductAttributeValueResponseDto> GetByIdAsync(Guid valueId)
    {
        var pav = await db.ProductAttributeValues
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == valueId);

        return pav == null ? throw new Exception("ProductAttributeValue not found") : MapToDto(pav);
    }

    public async Task<IEnumerable<ProductAttributeValueResponseDto>> GetByProductIdAsync(Guid productId)
    {
        var list = await db.ProductAttributeValues
            .Include(p => p.Translations)
            .Where(p => p.ProductId == productId)
            .ToListAsync();

        return list.Select(MapToDto);
    }

    #endregion CRUD

    #region Translations

    public async Task<ProductAttributeValueTranslationResponseDto> AddTranslationAsync(Guid valueId, CreateProductAttributeValueTranslationDto dto)
    {
        var pav = await db.ProductAttributeValues
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == valueId) ?? throw new Exception("ProductAttributeValue not found");

        if (pav.Translations.Any(t => t.LanguageCode == dto.LanguageCode))
            throw new Exception("Translation already exists");

        var translation = new ProductAttributeValueTranslation
        {
            LanguageCode = dto.LanguageCode,
            Value = dto.Value
        };

        pav.Translations.Add(translation);
        await db.SaveChangesAsync();

        return new ProductAttributeValueTranslationResponseDto
        {
            Id = translation.Id,
            LanguageCode = translation.LanguageCode,
            Value = translation.Value
        };
    }

    public async Task<ProductAttributeValueTranslationResponseDto> UpdateTranslationAsync(Guid valueId, CreateProductAttributeValueTranslationDto dto)
    {
        var pav = await db.ProductAttributeValues
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == valueId) ?? throw new Exception("ProductAttributeValue not found");

        var translation = pav.Translations.FirstOrDefault(t => t.LanguageCode == dto.LanguageCode) ?? throw new Exception("Translation not found");

        translation.Value = dto.Value;
        await db.SaveChangesAsync();

        return new ProductAttributeValueTranslationResponseDto
        {
            Id = translation.Id,
            LanguageCode = translation.LanguageCode,
            Value = translation.Value
        };
    }

    public async Task DeleteTranslationAsync(Guid valueId, string languageCode)
    {
        var pav = await db.ProductAttributeValues
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == valueId) ?? throw new Exception("ProductAttributeValue not found");

        var translation = pav.Translations.FirstOrDefault(t => t.LanguageCode == languageCode) ?? throw new Exception("Translation not found");

        pav.Translations.Remove(translation);
        await db.SaveChangesAsync();
    }

    public async Task<IEnumerable<ProductAttributeValueTranslationResponseDto>> GetTranslationsByValueIdAsync(Guid valueId)
    {
        var pav = await db.ProductAttributeValues
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == valueId);

        return pav == null
            ? throw new Exception("ProductAttributeValue not found")
            : pav.Translations.Select(t => new ProductAttributeValueTranslationResponseDto
            {
                Id = t.Id,
                LanguageCode = t.LanguageCode,
                Value = t.Value
            });
    }

    #endregion Translations

    #region Helpers

    private static ProductAttributeValueResponseDto MapToDto(ProductAttributeValue pav)
    {
        return new ProductAttributeValueResponseDto
        {
            Id = pav.Id,
            ProductAttributeId = pav.ProductAttributeId,
            StringValue = pav.StringValue,
            DecimalValue = pav.DecimalValue,
            IntValue = pav.IntValue,
            BoolValue = pav.BoolValue,
            Translations = [.. pav.Translations.Select(t => new ProductAttributeValueTranslationResponseDto
            {
                Id = t.Id,
                LanguageCode = t.LanguageCode,
                Value = t.Value
            })]
        };
    }

    #endregion Helpers
}