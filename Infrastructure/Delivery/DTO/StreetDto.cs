namespace LeveLEO.Infrastructure.Delivery.DTO;

/// <summary>Елемент Addresses з NP searchSettlementStreets.</summary>
public class StreetDto
{
    public string SettlementRef { get; set; } = null!;

    public string SettlementStreetRef { get; set; } = null!;

    public string? SettlementStreetDescription { get; set; }

    public string Present { get; set; } = null!;

    /// <summary>Ref типу вулиці з НП (GUID).</summary>
    public string StreetsType { get; set; } = null!;

    public string StreetsTypeDescription { get; set; } = null!;
}
