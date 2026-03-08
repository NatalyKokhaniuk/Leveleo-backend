using LeveLEO.Features.Products.Models;
using LeveLEO.Features.Products.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeveLEO.Features.Products.Controllers;

[ApiController]
[Route("api/products/{productId:guid}/media")]
[Authorize(Roles = "Admin,Moderator")]
public class ProductMediaController(IProductMediaService service) : ControllerBase
{
    #region Images

    [HttpPost("images")]
    public async Task<ActionResult<ProductImage>> AddImage(Guid productId, [FromQuery] string imageKey, [FromQuery] int? sortOrder = null)
    {
        var image = await service.AddImageAsync(productId, imageKey, sortOrder);
        return CreatedAtAction(nameof(GetImages), new { productId }, image);
    }

    [HttpDelete("images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(Guid imageId)
    {
        await service.DeleteImageAsync(imageId);
        return NoContent();
    }

    [HttpGet("images")]
    public async Task<ActionResult<List<ProductImage>>> GetImages(Guid productId)
    {
        var images = await service.GetImagesAsync(productId);
        return Ok(images);
    }

    #endregion Images

    #region Videos

    [HttpPost("videos")]
    public async Task<ActionResult<ProductVideo>> AddVideo(Guid productId, [FromQuery] string videoKey, [FromQuery] int? sortOrder = null)
    {
        var video = await service.AddVideoAsync(productId, videoKey, sortOrder);
        return CreatedAtAction(nameof(GetVideos), new { productId }, video);
    }

    [HttpDelete("videos/{videoId:guid}")]
    public async Task<IActionResult> DeleteVideo(Guid videoId)
    {
        await service.DeleteVideoAsync(videoId);
        return NoContent();
    }

    [HttpGet("videos")]
    public async Task<ActionResult<List<ProductVideo>>> GetVideos(Guid productId)
    {
        var videos = await service.GetVideosAsync(productId);
        return Ok(videos);
    }

    #endregion Videos
}