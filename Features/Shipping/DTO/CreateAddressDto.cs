// Features/Shipping/DTO/CreateAddressDto.cs
using LeveLEO.Features.Shipping.Models;

namespace LeveLEO.Features.Shipping.DTO;

public class CreateAddressDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string MiddleName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = null!;

    public DeliveryType DeliveryType { get; set; }

    // Для відділення
    public string? CityRef { get; set; }

    public string? CityName { get; set; }
    public string? WarehouseRef { get; set; }

    // Для адресної доставки
    public string? StreetRef { get; set; }

    public string? Street { get; set; }
    public string? House { get; set; }
    public string? Flat { get; set; }
    public string? Floor { get; set; }

    public string? AdditionalInfo { get; set; }
}