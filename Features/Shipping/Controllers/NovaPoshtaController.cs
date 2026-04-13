using LeveLEO.Infrastructure.Delivery;
using LeveLEO.Infrastructure.Delivery.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeveLEO.Features.Shipping.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NovaPoshtaController(INovaPoshtaService novaPoshtaService) : ControllerBase
{
    /// <summary>Онлайн-пошук населених пунктів (searchSettlements) — зручно для автокомпліту.</summary>
    [HttpGet("cities/search")]
    public async Task<ActionResult<List<CityDto>>> SearchCities(
        [FromQuery] string query,
        [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Ok(new List<CityDto>());
        }

        var cities = await novaPoshtaService.SearchCitiesAsync(query.Trim(), limit > 0 ? limit : 20);
        return Ok(cities);
    }

    /// <summary>Сторінка довідника населених пунктів НП (getSettlements). Перебирайте page, доки hasMore == true.</summary>
    [HttpGet("settlements")]
    public async Task<ActionResult<SettlementsPageDto>> GetSettlements(
        [FromQuery] int page = 1,
        [FromQuery] string? find = null)
    {
        var result = await novaPoshtaService.GetSettlementsPageAsync(page, find);
        return Ok(result);
    }

    /// <summary>Усі поштомати для обраного населеного пункту (Ref з search або довідника).</summary>
    [HttpGet("settlements/{settlementRef}/postomats")]
    public async Task<ActionResult<List<WarehouseDto>>> GetPostomats(string settlementRef)
    {
        if (string.IsNullOrWhiteSpace(settlementRef))
        {
            return BadRequest("settlementRef is required.");
        }

        var list = await novaPoshtaService.GetPostomatsBySettlementAsync(settlementRef.Trim());
        return Ok(list);
    }

    /// <summary>Усі відділення (не поштомати) з адресами для населеного пункту.</summary>
    [HttpGet("settlements/{settlementRef}/branches")]
    public async Task<ActionResult<List<WarehouseDto>>> GetBranches(string settlementRef)
    {
        if (string.IsNullOrWhiteSpace(settlementRef))
        {
            return BadRequest("settlementRef is required.");
        }

        var list = await novaPoshtaService.GetBranchWarehousesBySettlementAsync(settlementRef.Trim());
        return Ok(list);
    }
}
