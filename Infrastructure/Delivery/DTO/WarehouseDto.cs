using System.Text.Json.Serialization;
using LeveLEO.Infrastructure.Delivery.Json;

namespace LeveLEO.Infrastructure.Delivery.DTO;

public class WarehouseDto
{
    public string Ref { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Number { get; set; } = null!;
    public string CityRef { get; set; } = null!;
    public string CityDescription { get; set; } = string.Empty;
    public string SettlementRef { get; set; } = string.Empty;
    public string SettlementDescription { get; set; } = string.Empty;

    public string? ShortAddress { get; set; }

    /// <summary>UUID типу відділення з поля НП <c>TypeOfWarehouse</c> (текстового дубля в API немає — не додавати другу властивість з тим самим JSON-іменем).</summary>
    [JsonPropertyName("TypeOfWarehouse")]
    public string? TypeOfWarehouseRef { get; set; }

    /// <summary>З відповіді НП: <c>Branch</c>, <c>Postomat</c> тощо.</summary>
    public string? CategoryOfWarehouse { get; set; }

    [JsonConverter(typeof(NovaPoshtaDoubleConverter))]
    public double Latitude { get; set; }

    [JsonConverter(typeof(NovaPoshtaDoubleConverter))]
    public double Longitude { get; set; }
}
