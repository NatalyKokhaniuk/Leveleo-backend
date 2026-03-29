using LeveLEO.Features.ContactForms.DTO;
using LeveLEO.Features.ContactForms.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeveLEO.Features.ContactForms.Controllers;

[ApiController]
[Route("api/contact")]
public class ContactFormsController(
    IContactFormService contactFormService) : ControllerBase
{
    /// <summary>
    /// Надіслати форму зворотного зв'язку (доступно без авторизації)
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<ContactFormResponseDto>> Submit([FromBody] CreateContactFormDto dto)
    {
        var result = await contactFormService.SubmitAsync(dto);
        return Ok(result);
    }
}