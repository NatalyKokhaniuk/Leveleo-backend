namespace LeveLEO.Features.Shipping.DTO;

/// <summary>Відповідь для автокомпліту міст (без ключа JSON «ref», зручно для Angular/TS).</summary>
public class CityAutocompleteResponseDto
{
    /// <summary>Ref населеного пункту Нової пошти (те саме що раніше в полі Ref).</summary>
    public required string SettlementRef { get; set; }

    public required string Present { get; set; }
    public required string MainDescription { get; set; }

    /// <summary>Заголовок для option у випадаючому списку (fallback на mainDescription).</summary>
    public required string DisplayLabel { get; set; }
    public required string Area { get; set; }
    public required string Region { get; set; }
    public required string SettlementTypeCode { get; set; }

    /// <summary>Числовий опис кількості відділень з НП («1» або інше).</summary>
    public string WarehouseCountHint { get; set; } = "1";
}

/// <summary>Відділення / поштомат для випадаючого списку.</summary>
public class WarehouseAutocompleteResponseDto
{
    public required string WarehouseRef { get; set; }

    public required string Description { get; set; }
    public required string Number { get; set; }
    public required string CityRef { get; set; }
    public string CityDescription { get; set; } = "";

    /// <summary>Ref населеного пункту.</summary>
    public string SettlementRef { get; set; } = "";

    public string SettlementDescription { get; set; } = "";

    public string? ShortAddress { get; set; }
    public string? TypeOfWarehouseRef { get; set; }
    public string? TypeOfWarehouse { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
