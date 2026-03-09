using LeveLEO.Features.AttributeGroups.DTO;
using LeveLEO.Features.AttributeGroups.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeveLEO.Features.AttributeGroups.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttributeGroupsController(IAttributeGroupService service) : ControllerBase
{
    #region CRUD

    // GET api/attributegroups
    [HttpGet]
    public async Task<ActionResult<List<AttributeGroupResponseDto>>> GetAll()
    {
        var groups = await service.GetAllAsync();
        return Ok(groups);
    }

    // GET api/attributegroups/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AttributeGroupResponseDto>> GetById(Guid id)
    {
        var group = await service.GetByIdAsync(id);
        return Ok(group);
    }

    // GET api/attributegroups/slug/{slug}
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<AttributeGroupResponseDto>> GetBySlug(string slug)
    {
        var group = await service.GetBySlugAsync(slug);
        return Ok(group);
    }

    // GET api/attributegroups/search?q=...
    [HttpGet("search")]
    public async Task<ActionResult<List<AttributeGroupResponseDto>>> Search([FromQuery] string q)
    {
        var results = await service.SearchAsync(q);
        return Ok(results);
    }

    // POST api/attributegroups
    [HttpPost]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<AttributeGroupResponseDto>> Create(
        [FromBody] CreateAttributeGroupDto dto)
    {
        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT api/attributegroups/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<AttributeGroupResponseDto>> Update(
        Guid id,
        [FromBody] UpdateAttributeGroupDto dto)
    {
        var updated = await service.UpdateAsync(id, dto);
        return Ok(updated);
    }

    // DELETE api/attributegroups/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    #endregion CRUD

    #region Translations

    // POST api/attributegroups/{groupId}/translations
    [HttpPost("{groupId:guid}/translations")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<AttributeGroupTranslationResponseDto>> AddTranslation(
        Guid groupId,
        [FromBody] CreateAttributeGroupTranslationDto dto)
    {
        var created = await service.AddTranslationAsync(groupId, dto);
        return Ok(created);
    }

    // PUT api/attributegroups/{groupId}/translations
    [HttpPut("{groupId:guid}/translations")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<ActionResult<AttributeGroupTranslationResponseDto>> UpdateTranslation(
        Guid groupId,
        [FromBody] CreateAttributeGroupTranslationDto dto)
    {
        var updated = await service.UpdateTranslationAsync(groupId, dto);
        return Ok(updated);
    }

    // DELETE api/attributegroups/{groupId}/translations/{languageCode}
    [HttpDelete("{groupId:guid}/translations/{languageCode}")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<IActionResult> DeleteTranslation(
        Guid groupId,
        string languageCode)
    {
        await service.DeleteTranslationAsync(groupId, languageCode);
        return NoContent();
    }

    // GET api/attributegroups/{groupId}/translations/{languageCode}
    [HttpGet("{groupId:guid}/translations/{languageCode}")]
    public async Task<ActionResult<AttributeGroupTranslationResponseDto>> GetTranslation(
        Guid groupId,
        string languageCode)
    {
        var translation = await service.GetTranslationAsync(groupId, languageCode);
        return Ok(translation);
    }

    // GET api/attributegroups/{groupId}/translations
    [HttpGet("{groupId:guid}/translations")]
    public async Task<ActionResult<List<AttributeGroupTranslationResponseDto>>> GetTranslationsByGroup(
        Guid groupId)
    {
        var translations = await service.GetTranslationsByGroupIdAsync(groupId);
        return Ok(translations);
    }

    #endregion Translations
}