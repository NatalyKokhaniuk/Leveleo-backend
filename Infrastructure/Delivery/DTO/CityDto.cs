namespace LeveLEO.Infrastructure.Delivery.DTO;

/// <summary>Елемент Addresses з NP searchSettlements (поля за потреби доповнюються з fallback getSettlements).</summary>
public class CityDto
{
    public string Ref { get; set; } = null!;
    public string? DeliveryCity { get; set; }
    public string Present { get; set; } = null!;
    public string MainDescription { get; set; } = null!;
    public string Area { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string SettlementTypeCode { get; set; } = null!;
    public int Warehouses { get; set; }
    public bool AddressDeliveryAllowed { get; set; } = true;
    public bool StreetsAvailability { get; set; }
}
