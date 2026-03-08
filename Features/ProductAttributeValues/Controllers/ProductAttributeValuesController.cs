using LeveLEO.Features.ProductAttributeValues.DTO;
using LeveLEO.Features.ProductAttributeValues.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeveLEO.Features.ProductAttributeValues.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Moderator")]
public class ProductAttributeValuesController(IProductAttributeValueService service) : ControllerBase
{
    #region ATTRIBUTE VALUES

    [HttpGet("product/{productId:guid}")]
    public async Task<ActionResult<IEnumerable<ProductAttributeValueResponseDto>>> GetByProductId(Guid productId)
        => Ok(await service.GetByProductIdAsync(productId));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductAttributeValueResponseDto>> GetById(Guid id)
        => Ok(await service.GetByIdAsync(id));

    [HttpPost("product/{productId:guid}")]
    public async Task<ActionResult<ProductAttributeValueResponseDto>> Create(Guid productId, CreateProductAttributeValueDto dto)
    {
        var created = await service.CreateAsync(productId, dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductAttributeValueResponseDto>> Update(Guid id, UpdateProductAttributeValueDto dto)
        => Ok(await service.UpdateAsync(id, dto));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    #endregion ATTRIBUTE VALUES

    #region TRANSLATIONS

    [HttpPost("{valueId:guid}/translations")]
    public async Task<ActionResult<ProductAttributeValueTranslationResponseDto>> AddTranslation(
        Guid valueId,
        CreateProductAttributeValueTranslationDto dto)
        => Ok(await service.AddTranslationAsync(valueId, dto));

    [HttpPut("{valueId:guid}/translations")]
    public async Task<ActionResult<ProductAttributeValueTranslationResponseDto>> UpdateTranslation(
        Guid valueId,
        CreateProductAttributeValueTranslationDto dto)
        => Ok(await service.UpdateTranslationAsync(valueId, dto));

    [HttpDelete("{valueId:guid}/translations/{languageCode}")]
    public async Task<IActionResult> DeleteTranslation(
        Guid valueId,
        string languageCode)
    {
        await service.DeleteTranslationAsync(valueId, languageCode);
        return NoContent();
    }

    [HttpGet("{valueId:guid}/translations")]
    public async Task<ActionResult<IEnumerable<ProductAttributeValueTranslationResponseDto>>> GetTranslations(
        Guid valueId)
        => Ok(await service.GetTranslationsByValueIdAsync(valueId));

    #endregion TRANSLATIONS
}