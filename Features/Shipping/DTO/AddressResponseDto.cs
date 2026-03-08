using LeveLEO.Features.Shipping.Models;

namespace LeveLEO.Features.Shipping.DTO;

public class AddressResponseDto
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string MiddleName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = null!;

    public DeliveryType DeliveryType { get; set; }

    // Форматована адреса для відображення користувачу
    public string FormattedAddress { get; set; } = null!;

    // Деталі
    public string? CityName { get; set; }

    public string? WarehouseDescription { get; set; }
    public string? Street { get; set; }
    public string? House { get; set; }
    public string? Flat { get; set; }

    public string? AdditionalInfo { get; set; }
}