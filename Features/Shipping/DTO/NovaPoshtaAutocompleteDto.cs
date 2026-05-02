namespace LeveLEO.Features.Shipping.DTO;

/// <summary>Відповідь пошуку населених пунктів (як у відповіді НП searchSettlements) для фронтенду.</summary>
public class CityAutocompleteResponseDto
{
    /// <summary>Ref населеного пункту — для пошуку вулиць.</summary>
    public string Ref { get; set; } = null!;

    /// <summary>Ref «міста доставки» НП — для відділень / поштоматів (getWarehouses тощо).</summary>
    public string DeliveryCity { get; set; } = null!;

    public string Present { get; set; } = null!;
    public string MainDescription { get; set; } = null!;
    public string Area { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string SettlementTypeCode { get; set; } = null!;
    public int Warehouses { get; set; }
    public bool AddressDeliveryAllowed { get; set; }
    public bool StreetsAvailability { get; set; }
}

/// <summary>Елемент результату пошуку вулиць для фронта (AddressGeneral.searchSettlementStreets).</summary>
public class SettlementStreetSearchItemDto
{
    public string SettlementRef { get; set; } = null!;

    public string SettlementStreetRef { get; set; } = null!;

    public string Present { get; set; } = null!;

    public string StreetsType { get; set; } = null!;

    public string StreetsTypeDescription { get; set; } = null!;
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

/// <summary>
/// Спільний контракт елемента списку для <c>GET .../settlements/{{deliveryCity}}/branches</c>
/// та <c>GET .../settlements/{{deliveryCity}}/postomats</c> (camelCase у JSON API).
/// </summary>
public class SettlementNovaPoshtaWarehouseItemDto
{
    public string Ref { get; set; } = null!;

    /// <summary>Зазвичай <c>Branch</c> або <c>Postomat</c> (<see cref="WarehouseDto.CategoryOfWarehouse"/>).</summary>
    public string Type { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string ShortAddress { get; set; } = null!;
    public string CityRef { get; set; } = null!;
    public double Lat { get; set; }
    public double Lng { get; set; }
}
