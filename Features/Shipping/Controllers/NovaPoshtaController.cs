using LeveLEO.Features.Shipping.DTO;
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
    private static CityAutocompleteResponseDto MapCity(CityDto c) => new()
    {
        Ref = c.Ref,
        DeliveryCity = c.DeliveryCity ?? string.Empty,
        Present = c.Present,
        MainDescription = c.MainDescription,
        Area = c.Area,
        Region = c.Region,
        SettlementTypeCode = c.SettlementTypeCode,
        Warehouses = c.Warehouses,
        AddressDeliveryAllowed = c.AddressDeliveryAllowed,
        StreetsAvailability = c.StreetsAvailability
    };

    private static SettlementNovaPoshtaWarehouseItemDto MapBranchContract(WarehouseDto w) => new()
    {
        Ref = w.Ref,
        Type = string.IsNullOrEmpty(w.CategoryOfWarehouse) ? "Branch" : w.CategoryOfWarehouse,
        Name = w.Description,
        ShortAddress = w.ShortAddress ?? string.Empty,
        CityRef = w.CityRef,
        Lat = w.Latitude,
        Lng = w.Longitude
    };

    private static SettlementNovaPoshtaWarehouseItemDto MapPostomatContract(WarehouseDto w) => new()
    {
        Ref = w.Ref,
        Type = string.IsNullOrEmpty(w.CategoryOfWarehouse) ? "Postomat" : w.CategoryOfWarehouse,
        Name = w.Description,
        ShortAddress = w.ShortAddress ?? string.Empty,
        CityRef = w.CityRef,
        Lat = w.Latitude,
        Lng = w.Longitude
    };

    private static SettlementStreetSearchItemDto MapStreetSearchItem(StreetDto s) => new()
    {
        SettlementRef = s.SettlementRef,
        SettlementStreetRef = s.SettlementStreetRef,
        Present = s.Present,
        StreetsType = s.StreetsType,
        StreetsTypeDescription = s.StreetsTypeDescription
    };

    /// <summary>Пошук вулиць за Ref населеного пункту (як із <c>cities/search</c> поле <c>ref</c>).</summary>
    [HttpGet("settlements/{settlementRef}/streets/search")]
    public async Task<ActionResult<List<SettlementStreetSearchItemDto>>> SearchStreets(
        string settlementRef,
        [FromQuery] string query,
        [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(settlementRef))
        {
            return BadRequest("settlementRef is required.");
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return Ok(new List<SettlementStreetSearchItemDto>());
        }

        var list = await novaPoshtaService.SearchStreetsAsync(
            settlementRef.Trim(),
            query.Trim(),
            limit > 0 ? limit : 20);

        return Ok(list.ConvertAll(MapStreetSearchItem));
    }

    /// <summary>
    /// Онлайн-пошук населених пунктів (searchSettlements).
    /// Відповідь збігається з формою НП: <c>ref</c> для вулиць, <c>deliveryCity</c> для відділень/поштоматів у <c>/settlements/{{deliveryCity}}/…</c>.
    /// Записи без обох ref відфільтровані.
    /// </summary>
    [HttpGet("cities/search")]
    public async Task<ActionResult<List<CityAutocompleteResponseDto>>> SearchCities(
        [FromQuery] string query,
        [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Ok(new List<CityAutocompleteResponseDto>());
        }

        var cities = await novaPoshtaService.SearchCitiesAsync(query.Trim(), limit > 0 ? limit : 20);
        var dto = cities
            .ConvertAll(MapCity)
            .Where(x => !string.IsNullOrWhiteSpace(x.Ref) && !string.IsNullOrWhiteSpace(x.DeliveryCity))
            .ToList();
        return Ok(dto);
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

    /// <summary>
    /// Лише поштомати: після <c>getWarehouses</c> залишаються точки, класифіковані як поштомат
    /// (довідник <c>getWarehouseTypes</c>, інакше — <c>CategoryOfWarehouse</c> / опис).
    /// Відділення сюди не потрапляють — див. <see cref="GetBranches"/>.
    /// У шляху — ref міста доставки або населеного пункту (як у пошуку міст).
    /// </summary>
    [HttpGet("settlements/{settlementRef}/postomats")]
    public async Task<ActionResult<List<SettlementNovaPoshtaWarehouseItemDto>>> GetPostomats(
        string settlementRef,
        [FromQuery] string? findByString = null)
    {
        if (string.IsNullOrWhiteSpace(settlementRef))
        {
            return BadRequest("settlementRef is required.");
        }

        var list = await novaPoshtaService.GetPostomatsBySettlementAsync(
            settlementRef.Trim(),
            string.IsNullOrWhiteSpace(findByString) ? null : findByString.Trim());

        return Ok(list.ConvertAll(MapPostomatContract));
    }

    /// <summary>
    /// Відділення та інші точки, що не є поштоматами: той самий запит до НП, що й для поштоматів,
    /// але з відповіді прибираються поштомати (див. <see cref="GetPostomats"/>).
    /// Якщо є <c>findByString</c> — пошук як у кабінеті НП по місту.
    /// </summary>
    [HttpGet("settlements/{settlementRef}/branches")]
    public async Task<ActionResult<List<SettlementNovaPoshtaWarehouseItemDto>>> GetBranches(
        string settlementRef,
        [FromQuery] string? findByString = null)
    {
        if (string.IsNullOrWhiteSpace(settlementRef))
        {
            return BadRequest("settlementRef is required.");
        }

        var list = await novaPoshtaService.GetBranchWarehousesBySettlementAsync(
            settlementRef.Trim(),
            string.IsNullOrWhiteSpace(findByString) ? null : findByString.Trim());

        return Ok(list.ConvertAll(MapBranchContract));
    }
}
