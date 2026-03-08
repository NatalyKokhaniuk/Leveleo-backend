using LeveLEO.Features.Categories.DTO;
using LeveLEO.Features.Categories.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeveLEO.Features.Categories.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Moderator")]
public class CategoriesController(ICategoryService service) : ControllerBase
{
    private readonly ICategoryService _service = service;

    [HttpPost]
    public async Task<ActionResult<CategoryResponseDto>> Create([FromBody] CreateCategoryDto dto)
        => Ok(await _service.CreateAsync(dto));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryResponseDto>> Update(Guid id, [FromBody] UpdateCategoryDto dto)
        => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryResponseDto>> GetById(Guid id)
        => Ok(await _service.GetByIdAsync(id));

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<CategoryResponseDto>> GetBySlug(string slug)
        => Ok(await _service.GetBySlugAsync(slug));

    [HttpGet]
    public async Task<ActionResult<List<CategoryResponseDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("search")]
    public async Task<ActionResult<List<CategoryResponseDto>>> Search([FromQuery] string query)
        => Ok(await _service.SearchAsync(query));

    [HttpGet("{id:guid}/breadcrumbs")]
    public async Task<ActionResult<CategoryBreadcrumbsDto>> GetBreadcrumbs(Guid id)
        => Ok(await _service.GetBreadcrumbsAsync(id));

    // -------------------- TRANSLATIONS --------------------

    [HttpPost("{categoryId:guid}/translations")]
    public async Task<IActionResult> AddTranslation(Guid categoryId, [FromBody] CreateCategoryTranslationDto dto)
    {
        await _service.AddTranslationAsync(categoryId, dto);
        return NoContent();
    }

    [HttpPut("{categoryId:guid}/translations")]
    public async Task<IActionResult> UpdateTranslation(Guid categoryId, [FromBody] CreateCategoryTranslationDto dto)
    {
        await _service.UpdateTranslationAsync(categoryId, dto);
        return NoContent();
    }

    [HttpDelete("{categoryId:guid}/translations/{languageCode}")]
    public async Task<IActionResult> DeleteTranslation(Guid categoryId, string languageCode)
    {
        await _service.DeleteTranslationAsync(categoryId, languageCode);
        return NoContent();
    }

    [HttpGet("{categoryId:guid}/translations")]
    public async Task<ActionResult<List<CategoryTranslationResponseDto>>> GetTranslationsByCategoryId(Guid categoryId)
    {
        var result = await _service.GetTranslationsByCategoryIdAsync(categoryId);
        return Ok(result);
    }

    [HttpGet("{categoryId:guid}/translations/{languageCode}")]
    public async Task<ActionResult<CategoryTranslationResponseDto>> GetTranslationById(Guid categoryId, string languageCode)
    {
        var result = await _service.GetTranslationByIdAsync(categoryId, languageCode);
        return Ok(result);
    }
}