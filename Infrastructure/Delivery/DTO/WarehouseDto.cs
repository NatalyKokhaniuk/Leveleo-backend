namespace LeveLEO.Infrastructure.Delivery.DTO;

public class WarehouseDto
{
    public string Ref { get; set; } = null!;
    public string Description { get; set; } = null!; // "Відділення №1: вул. Хрещатик, 1"
    public string Number { get; set; } = null!; // "1"
    public string CityRef { get; set; } = null!;
    public string CityDescription { get; set; } = null!;
    public string SettlementRef { get; set; } = null!;
    public string SettlementDescription { get; set; } = null!;

    /// <summary>Коротка адреса (якщо повертає API).</summary>
    public string? ShortAddress { get; set; }

    /// <summary>Ref типу відділення (поштомат, відділення тощо).</summary>
    public string? TypeOfWarehouseRef { get; set; }

    /// <summary>Назва типу відділення.</summary>
    public string? TypeOfWarehouse { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }
}