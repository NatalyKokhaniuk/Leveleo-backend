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
    [HttpGet("myaddresses")]
    public async Task<ActionResult<List<AddressResponseDto>>> GetUserAddresses()
    {
        var userId = User.Identity!.Name!;
        var addresses = await addressService.GetUserAddressesAsync(userId);
        return Ok(addresses);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AddressResponseDto>> GetAddressById(Guid id)
    {
        var address = await addressService.GetAddressByIdAsync(id);
        return Ok(address);
    }

    [HttpPost]
    public async Task<ActionResult<AddressResponseDto>> CreateAddress([FromBody] CreateAddressDto dto)
    {
        var userId = User.Identity!.Name!;
        var address = await addressService.CreateAddressAsync(userId, dto);
        return CreatedAtAction(nameof(GetAddressById), new { id = address.Id }, address);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AddressResponseDto>> UpdateAddress(Guid id, [FromBody] UpdateAddressDto dto)
    {
        var userId = User.Identity!.Name!;
        var updatedAddress = await addressService.UpdateAddressAsync(userId, id, dto);
        return Ok(updatedAddress);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid id)
    {
        var userId = User.Identity!.Name!;
        await addressService.DeleteAddressAsync(userId, id);
        return NoContent();
    }

    [HttpPost("{id:guid}/default")]
    public async Task<ActionResult<AddressResponseDto>> SetDefaultAddress(Guid id)
    {
        var userId = User.Identity!.Name!;
        var address = await addressService.SetDefaultAddressAsync(userId, id);
        return Ok(address);
    }
}