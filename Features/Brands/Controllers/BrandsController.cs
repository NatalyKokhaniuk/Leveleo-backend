using LeveLEO.Features.Brands.DTO;
using LeveLEO.Features.Brands.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeveLEO.Features.Brands.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrandsController(IBrandService service) : ControllerBase
{
    private readonly IBrandService _service = service;

    // -------------------- BRANDS --------------------

    [HttpPost]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<BrandResponseDto>> Create([FromBody] CreateBrandDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { brandId = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BrandResponseDto>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<BrandResponseDto>> GetBySlug(string slug)
    {
        var result = await _service.GetBySlugAsync(slug);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<BrandResponseDto>>> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<BrandResponseDto>>> Search([FromQuery] string query)
    {
        var result = await _service.SearchAsync(query);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<BrandResponseDto>> Update(Guid id, [FromBody] UpdateBrandDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    // -------------------- TRANSLATIONS --------------------

    [HttpPost("{brandId:guid}/translations")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> AddTranslation(Guid brandId, [FromBody] CreateBrandTranslationDto dto)
    {
        await _service.AddTranslationAsync(brandId, dto);
        return NoContent();
    }

    [HttpPut("{brandId:guid}/translations")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> UpdateTranslation(Guid brandId, [FromBody] CreateBrandTranslationDto dto)
    {
        await _service.UpdateTranslationAsync(brandId, dto);
        return NoContent();
    }

    [HttpDelete("{brandId:guid}/translations/{languageCode}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> DeleteTranslation(Guid brandId, string languageCode)
    {
        await _service.DeleteTranslationAsync(brandId, languageCode);
        return NoContent();
    }

    [HttpGet("{brandId:guid}/translations")]
    public async Task<ActionResult<List<BrandTranslationResponseDto>>> GetTranslationsByBrandId(Guid brandId)
    {
        var result = await _service.GetTranslationsByBrandIdAsync(brandId);
        return Ok(result);
    }

    [HttpGet("{brandId:guid}/translations/{languageCode}")]
    public async Task<ActionResult<BrandTranslationResponseDto>> GetTranslationById(Guid brandId, string languageCode)
    {
        var result = await _service.GetTranslationByIdAsync(brandId, languageCode);
        return Ok(result);
    }
}