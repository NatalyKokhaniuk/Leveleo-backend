using LeveLEO.Features.Shipping.DTO;
using LeveLEO.Features.Shipping.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeveLEO.Features.Shipping.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DeliveryController : ControllerBase
{
    private readonly IDeliveryService _deliveryService;

    public DeliveryController(IDeliveryService deliveryService)
    {
        _deliveryService = deliveryService;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeliveryResponseDto>> GetDeliveryById(Guid id)
    {
        var delivery = await _deliveryService.GetDeliveryByIdAsync(id);
        return Ok(delivery);
    }

    [HttpGet("order/{orderId:guid}")]
    public async Task<ActionResult<DeliveryResponseDto>> GetDeliveryByOrderId(Guid orderId)
    {
        var delivery = await _deliveryService.GetDeliveryByOrderIdAsync(orderId);
        return Ok(delivery);
    }

    [HttpGet("order-number/{orderNumber}")]
    public async Task<ActionResult<DeliveryResponseDto>> GetDeliveryByOrderNumber(string orderNumber)
    {
        var delivery = await _deliveryService.GetDeliveryByOrderNumberAsync(orderNumber);
        return Ok(delivery);
    }

    [HttpGet("tracking/{trackingNumber}")]
    public async Task<ActionResult<DeliveryResponseDto>> GetDeliveryByTrackingNumber(string trackingNumber)
    {
        var delivery = await _deliveryService.GetDeliveryByTrackingNumberAsync(trackingNumber);
        return Ok(delivery);
    }

    [HttpPost("create/{orderId:guid}")]
    public async Task<ActionResult<DeliveryResponseDto>> CreateDelivery(Guid orderId)
    {
        var delivery = await _deliveryService.CreateDeliveryAsync(orderId);
        return CreatedAtAction(nameof(GetDeliveryById), new { id = delivery.Id }, delivery);
    }

    [HttpPatch("{deliveryId:guid}/status")]
    public async Task<ActionResult<DeliveryResponseDto>> UpdateDeliveryStatus(Guid deliveryId)
    {
        var updatedDelivery = await _deliveryService.UpdateDeliveryStatusAsync(deliveryId);
        return Ok(updatedDelivery);
    }

    [HttpPost("{deliveryId:guid}/cancel")]
    public async Task<IActionResult> CancelDelivery(Guid deliveryId)
    {
        var result = await _deliveryService.CancelDeliveryAsync(deliveryId);
        return result ? NoContent() : BadRequest("Failed to cancel delivery.");
    }

    [HttpGet("{deliveryId:guid}/tracking-history")]
    public async Task<ActionResult<List<object>>> GetTrackingHistory(Guid deliveryId)
    {
        var history = await _deliveryService.GetTrackingHistoryAsync(deliveryId);
        return Ok(history);
    }
}