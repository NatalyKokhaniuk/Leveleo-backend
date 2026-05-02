using LeveLEO.Features.Shipping.Models;

namespace LeveLEO.Features.Shipping.DTO;

/// <summary>
/// Публічна відповідь API для фронтенду (POST/PUT/GET адреси, вкладена адреса у замовленні/доставці).
/// </summary>
public class AddressResponseDto
{
    public Guid Id { get; set; }

    /// <summary>Чи ця адреса обрана за замовчуванням для поточного користувача (лише в профілі адрес; у замовленнях зазвичай false).</summary>
    public bool IsDefault { get; set; }

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string MiddleName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = null!;

    public DeliveryType DeliveryType { get; set; }

    public string FormattedAddress { get; set; } = null!;

    public string? CityName { get; set; }

    public string? WarehouseRef { get; set; }

    public string? WarehouseDescription { get; set; }

    public string? PostomatRef { get; set; }

    public string? PostomatDescription { get; set; }

    public string? Street { get; set; }

    public string? House { get; set; }

    public string? Flat { get; set; }

    public string? AdditionalInfo { get; set; }
}
