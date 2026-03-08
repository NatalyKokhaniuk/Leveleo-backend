// Features/Shipping/Models/Address.cs
using LeveLEO.Models;

namespace LeveLEO.Features.Shipping.Models;

/// <summary>
/// Адреса доставки з інтеграцією Нової Пошти
/// </summary>
public class Address : ITimestamped
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // === Дані отримувача ===
    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;
    public string MiddleName { get; set; } = string.Empty; // По батькові
    public string PhoneNumber { get; set; } = null!; // Формат: +380XXXXXXXXX

    // === Тип доставки ===
    public DeliveryType DeliveryType { get; set; } = DeliveryType.Warehouse;

    // === Дані Нової Пошти (для відділення) ===
    public string? CityRef { get; set; } // Ref міста з НП API

    public string? CityName { get; set; } // Назва міста
    public string? WarehouseRef { get; set; } // Ref відділення з НП API
    public string? WarehouseNumber { get; set; } // Номер відділення (напр. "1", "25")
    public string? WarehouseDescription { get; set; } // Повна адреса відділення

    // === Дані для адресної доставки ===
    public string? StreetRef { get; set; } // Ref вулиці з НП API

    public string? Street { get; set; } // Назва вулиці
    public string? House { get; set; } // Номер будинку
    public string? Flat { get; set; } // Номер квартири
    public string? Floor { get; set; } // Поверх

    // === Додаткова інформація ===
    public string? PostalCode { get; set; } // Поштовий індекс (опційно для НП)

    public string? AdditionalInfo { get; set; } // Коментар до доставки

    // === Поштомат (якщо буде) ===
    public string? PostomatRef { get; set; }

    public string? PostomatDescription { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Тип доставки
/// </summary>
public enum DeliveryType
{
    Warehouse,        // На відділення (самовивіз)
    Doors,            // Адресна доставка (кур'єр)
    Postomat          // Поштомат
}