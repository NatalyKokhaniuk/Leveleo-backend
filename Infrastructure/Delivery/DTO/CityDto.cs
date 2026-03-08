namespace LeveLEO.Infrastructure.Delivery.DTO;

public class CityDto
{
    public string Ref { get; set; } = null!;
    public string Present { get; set; } = null!; // "Київ, Київська обл."
    public string MainDescription { get; set; } = null!; // "Київ"
    public string Area { get; set; } = null!; // "Київська"
    public string Region { get; set; } = null!;
    public string SettlementTypeCode { get; set; } = null!; // "м."
    public string Warehouse { get; set; } = "1"; // К-сть відділень
}