using LeveLEO.Infrastructure.Media.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LeveLEO.Infrastructure.Media.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MediaController(IMediaService mediaService) : ControllerBase
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm(Name = "file")] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "FILE_REQUIRED" });

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";

        var key = await mediaService.UploadFileAsync(file.OpenReadStream(), fileName, file.ContentType);

        // тимчасовий pre-signed URL на 30 хв
        //var tempUrl = await _mediaService.GetFileUrlAsync(fileName, TimeSpan.FromMinutes(30));
        // Генеруємо публічне посилання
        var url = mediaService.GetPermanentUrl(key);

        return Ok(new
        {
            fileName,
            key, // для збереження/логіки
            url
            //tempUrl       // для одразу на фронтенді
        });
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key)
    {
        await mediaService.DeleteFileAsync(key);
        return Ok(new { success = true });
    }

    /// <summary>
    /// Повертає тимчасовий pre-signed URL для ключа в сховищі (для &lt;img src&gt;, превʼю тощо).
    /// Доступно без авторизації: ключі приходять у публічних відповідях API (наприклад, зображення товарів).
    /// </summary>
    [HttpGet("url")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSignedUrl(
        [FromQuery] string key,
        [FromQuery] int expiresMinutes = 30)
    {
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest(new { message = "KEY_REQUIRED" });
        if (expiresMinutes is < 1 or > 10080) // макс. 7 днів, підлаштуйте під політику
            return BadRequest(new { message = "INVALID_EXPIRES" });
        var url = await mediaService.GetFileUrlAsync(key, TimeSpan.FromMinutes(expiresMinutes));
        return Ok(new { key, url });
    }
}