using System.Security.Claims;
using LeveLEO.Features.Shipping.DTO;
using LeveLEO.Features.Shipping.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeveLEO.Features.Shipping.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Всі методи доступні тільки автентифікованим
public class AddressController(IAddressService addressService) : ControllerBase
{
    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new ApiException(
            "UNAUTHORIZED",
            "User identifier is missing from token.",
            401);

    [HttpGet("myaddresses")]
    public async Task<ActionResult<List<AddressResponseDto>>> GetUserAddresses()
    {
        var addresses = await addressService.GetUserAddressesAsync(CurrentUserId);
        return Ok(addresses);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AddressResponseDto>> GetAddressById(Guid id)
    {
        var address = await addressService.GetAddressByIdAsync(CurrentUserId, id);
        return Ok(address);
    }

    [HttpPost]
    public async Task<ActionResult<AddressResponseDto>> CreateAddress([FromBody] CreateAddressDto dto)
    {
        var address = await addressService.CreateAddressAsync(CurrentUserId, dto);
        return CreatedAtAction(nameof(GetAddressById), new { id = address.Id }, address);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AddressResponseDto>> UpdateAddress(Guid id, [FromBody] UpdateAddressDto dto)
    {
        var updatedAddress = await addressService.UpdateAddressAsync(CurrentUserId, id, dto);
        return Ok(updatedAddress);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid id)
    {
        await addressService.DeleteAddressAsync(CurrentUserId, id);
        return NoContent();
    }

    [HttpPost("{id:guid}/default")]
    public async Task<ActionResult<AddressResponseDto>> SetDefaultAddress(Guid id)
    {
        var address = await addressService.SetDefaultAddressAsync(CurrentUserId, id);
        return Ok(address);
    }
}