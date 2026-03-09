using LeveLEO.Features.ProductAttributes.DTO;
using LeveLEO.Features.ProductAttributes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeveLEO.Features.ProductAttributes.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductAttributesController(IProductAttributeService service) : ControllerBase
{
    #region ATTRIBUTES

    [HttpGet]
    public async Task<ActionResult<List<ProductAttributeResponseDto>>> GetAll()
        => Ok(await service.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductAttributeResponseDto>> GetById(Guid id)
        => Ok(await service.GetByIdAsync(id));

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<ProductAttributeResponseDto>> GetBySlug(string slug)
        => Ok(await service.GetBySlugAsync(slug));

    [HttpGet("search")]
    public async Task<ActionResult<List<ProductAttributeResponseDto>>> Search([FromQuery] string query)
        => Ok(await service.SearchAsync(query));

    [HttpGet("group/{groupId:guid}")]
    public async Task<ActionResult<List<ProductAttributeResponseDto>>> GetByGroupId(Guid groupId)
        => Ok(await service.GetByGroupIdAsync(groupId));

    [HttpGet("group/slug/{groupSlug}")]
    public async Task<ActionResult<List<ProductAttributeResponseDto>>> GetByGroupSlug(string groupSlug)
        => Ok(await service.GetByGroupSlugAsync(groupSlug));

    [HttpPost]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<ProductAttributeResponseDto>> Create(CreateProductAttributeDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<ProductAttributeResponseDto>> Update(Guid id, UpdateProductAttributeDto dto)
        => Ok(await service.UpdateAsync(id, dto));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    #endregion ATTRIBUTES

    #region TRANSLATIONS

    // POST api/productattributes/{attributeId}/translations
    [HttpPost("{attributeId:guid}/translations")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<ProductAttributeTranslationResponseDto>> AddTranslation(
        Guid attributeId,
        CreateProductAttributeTranslationDto dto)
        => Ok(await service.AddTranslationAsync(attributeId, dto));

    // PUT api/productattributes/{attributeId}/translations
    [HttpPut("{attributeId:guid}/translations")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<ProductAttributeTranslationResponseDto>> UpdateTranslation(
        Guid attributeId,
        CreateProductAttributeTranslationDto dto)
        => Ok(await service.UpdateTranslationAsync(attributeId, dto));

    // DELETE api/productattributes/{attributeId}/translations/{languageCode}
    [HttpDelete("{attributeId:guid}/translations/{languageCode}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> DeleteTranslation(
        Guid attributeId,
        string languageCode)
    {
        await service.DeleteTranslationAsync(attributeId, languageCode);
        return NoContent();
    }

    // GET api/productattributes/{attributeId}/translations/{languageCode}
    [HttpGet("{attributeId:guid}/translations/{languageCode}")]
    public async Task<ActionResult<ProductAttributeTranslationResponseDto>> GetTranslation(
        Guid attributeId,
        string languageCode)
        => Ok(await service.GetTranslationAsync(attributeId, languageCode));

    // GET api/productattributes/{attributeId}/translations
    [HttpGet("{attributeId:guid}/translations")]
    public async Task<ActionResult<List<ProductAttributeTranslationResponseDto>>> GetTranslations(
        Guid attributeId)
        => Ok(await service.GetTranslationsByAttributeIdAsync(attributeId));

    #endregion TRANSLATIONS
}