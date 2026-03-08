using Amazon.S3.Model;
using LeveLEO.Features.Products.DTO;
using LeveLEO.Features.Products.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace LeveLEO.Features.Products.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductService service) : ControllerBase
{
    // Кэшированный экземпляр JsonSerializerOptions для повторного использования (фикс для CA1869).
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    #region CRUD

    [HttpPost]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<ProductResponseDto>> Create([FromBody] CreateProductDto dto)
    {
        var result = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { productId = result.Id }, result);
    }

    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<ProductResponseDto>> GetById(Guid productId)
    {
        var result = await service.GetByIdAsync(productId);
        return Ok(result);
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<ProductResponseDto>> GetBySlug(string slug)
    {
        var result = await service.GetBySlugAsync(slug);
        return Ok(result);
    }

    [HttpPut("{productId:guid}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<ProductResponseDto>> Update(Guid productId, [FromBody] UpdateProductDto dto)
    {
        var result = await service.UpdateAsync(productId, dto);
        return Ok(result);
    }

    [HttpDelete("{productId:guid}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> Delete(Guid productId)
    {
        await service.DeleteAsync(productId);
        return NoContent();
    }

    #endregion CRUD

    #region TRANSLATIONS

    [HttpPost("{productId:guid}/translations")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<ProductTranslationResponseDto>> AddTranslation(Guid productId, [FromBody] ProductTranslationDto dto)
    {
        var result = await service.AddTranslationAsync(productId, dto);
        return CreatedAtAction(nameof(GetTranslationById), new { productId, languageCode = result.LanguageCode }, result);
    }

    [HttpPut("{productId:guid}/translations")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<ProductTranslationResponseDto>> UpdateTranslation(Guid productId, [FromBody] ProductTranslationDto dto)
    {
        var result = await service.UpdateTranslationAsync(productId, dto);
        return Ok(result);
    }

    [HttpDelete("{productId:guid}/translations/{languageCode}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> DeleteTranslation(Guid productId, string languageCode)
    {
        await service.DeleteTranslationAsync(productId, languageCode);
        return NoContent();
    }

    [HttpGet("{productId:guid}/translations")]
    public async Task<ActionResult<List<ProductTranslationResponseDto>>> GetTranslations(Guid productId)
    {
        var result = await service.GetByIdAsync(productId); // для повного продукту з перекладами
        return Ok(result.Translations);
    }

    [HttpGet("{productId:guid}/translations/{languageCode}")]
    public async Task<ActionResult<ProductTranslationResponseDto>> GetTranslationById(Guid productId, string languageCode)
    {
        var product = await service.GetByIdAsync(productId);
        var translation = product.Translations.FirstOrDefault(t => t.LanguageCode == languageCode);
        if (translation == null)
            return NotFound();
        return Ok(translation);
    }

    #endregion TRANSLATIONS

    #region LIST & SEARCH

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ProductResponseDto>>> GetAll([FromQuery] string? filters)
    {
        ProductFilterDto filterDto;

        if (!string.IsNullOrWhiteSpace(filters))
        {
            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(filters));
                filterDto = JsonSerializer.Deserialize<ProductFilterDto>(json, s_jsonOptions)
                            ?? new ProductFilterDto();
            }
            catch
            {
                throw new ApiException(
                    "INVALID_FILTERS_PARAMETER",
                    "Invalid filters parameter. Ensure it's a valid Base64-encoded JSON string.",
                    400 // краще 400, бо це Bad Request
                );
            }
        }
        else
        {
            filterDto = new ProductFilterDto();
        }

        var result = await service.GetAllAsync(filterDto);
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<ActionResult<PagedResultDto<ProductResponseDto>>> Search([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await service.SearchAsync(query, page, pageSize);
        return Ok(result);
    }

    #endregion LIST & SEARCH
}