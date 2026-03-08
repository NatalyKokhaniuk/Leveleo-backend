using LeveLEO.Data;
using LeveLEO.Features.Shipping.DTO;
using LeveLEO.Features.Shipping.Models;
using Microsoft.EntityFrameworkCore;

namespace LeveLEO.Features.Shipping.Services;

public class AddressService(AppDbContext db) : IAddressService
{
    public async Task<AddressResponseDto> CreateAddressAsync(string userId, CreateAddressDto dto)
    {
        // Валідація
        ValidateAddress(dto);

        var address = new Address
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            MiddleName = dto.MiddleName,
            PhoneNumber = dto.PhoneNumber,
            DeliveryType = dto.DeliveryType,

            CityRef = dto.CityRef,
            CityName = dto.CityName,
            WarehouseRef = dto.WarehouseRef,

            StreetRef = dto.StreetRef,
            Street = dto.Street,
            House = dto.House,
            Flat = dto.Flat,
            Floor = dto.Floor,

            AdditionalInfo = dto.AdditionalInfo,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.Addresses.Add(address);

        // Зв'язуємо з користувачем через UserAddress
        var userAddress = new UserAddress
        {
            UserId = userId,
            AddressId = address.Id
        };

        db.UserAddresses.Add(userAddress);
        await db.SaveChangesAsync();

        return MapToDto(address);
    }

    public async Task<AddressResponseDto> UpdateAddressAsync(string userId, Guid addressId, UpdateAddressDto dto)
    {
        // Перевіряємо, що адреса належить користувачу
        var userAddress = await db.UserAddresses
            .Include(ua => ua.Address)
            .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AddressId == addressId)
            ?? throw new ApiException(
                "ADDRESS_NOT_FOUND",
                "Address not found or does not belong to user.",
                404
            );

        var address = userAddress.Address;

        // Оновлюємо поля
        if (dto.FirstName.HasValue)
        {
            if (string.IsNullOrWhiteSpace(dto.FirstName.Value))
                throw new ApiException("VALIDATION_ERROR", "FirstName cannot be empty.", 400);

            address.FirstName = dto.FirstName.Value;
        }

        if (dto.LastName.HasValue)
        {
            if (string.IsNullOrWhiteSpace(dto.LastName.Value))
                throw new ApiException("VALIDATION_ERROR", "LastName cannot be empty.", 400);

            address.LastName = dto.LastName.Value;
        }

        if (dto.MiddleName.HasValue)
            address.MiddleName = dto.MiddleName.Value ?? string.Empty;

        if (dto.PhoneNumber.HasValue)
        {
            if (string.IsNullOrWhiteSpace(dto.PhoneNumber.Value))
                throw new ApiException("VALIDATION_ERROR", "PhoneNumber cannot be empty.", 400);
            address.PhoneNumber = dto.PhoneNumber.Value;
        }

        if (dto.DeliveryType.HasValue)
            address.DeliveryType = dto.DeliveryType.Value;

        if (dto.CityRef.HasValue)
            address.CityRef = dto.CityRef.Value;

        if (dto.CityName.HasValue)
            address.CityName = dto.CityName.Value;

        if (dto.WarehouseRef.HasValue)
            address.WarehouseRef = dto.WarehouseRef.Value;

        if (dto.StreetRef.HasValue)
            address.StreetRef = dto.StreetRef.Value;

        if (dto.Street.HasValue)
            address.Street = dto.Street.Value;

        if (dto.House.HasValue)
            address.House = dto.House.Value;

        if (dto.Flat.HasValue)
            address.Flat = dto.Flat.Value;

        if (dto.Floor.HasValue)
            address.Floor = dto.Floor.Value;

        if (dto.AdditionalInfo.HasValue)
            address.AdditionalInfo = dto.AdditionalInfo.Value;

        address.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return MapToDto(address);
    }

    public async Task DeleteAddressAsync(string userId, Guid addressId)
    {
        var userAddress = await db.UserAddresses
            .Include(ua => ua.Address)
            .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AddressId == addressId)
            ?? throw new ApiException(
                "ADDRESS_NOT_FOUND",
                "Address not found or does not belong to user.",
                404
            );

        // Видаляємо зв'язок
        db.UserAddresses.Remove(userAddress);

        // Видаляємо адресу (якщо вона не використовується в замовленнях)
        var usedInOrders = await db.Orders.AnyAsync(o => o.AddressId == addressId);
        if (!usedInOrders)
        {
            db.Addresses.Remove(userAddress.Address);
        }

        await db.SaveChangesAsync();
    }

    public async Task<AddressResponseDto> GetAddressByIdAsync(Guid addressId)
    {
        var address = await db.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId)
            ?? throw new ApiException("ADDRESS_NOT_FOUND", "Address not found.", 404);

        return MapToDto(address);
    }

    public async Task<List<AddressResponseDto>> GetUserAddressesAsync(string userId)
    {
        var addresses = await db.UserAddresses
            .Include(ua => ua.Address)
            .Where(ua => ua.UserId == userId)
            .Select(ua => ua.Address)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return [.. addresses.Select(MapToDto)];
    }

    public async Task<AddressResponseDto> SetDefaultAddressAsync(string userId, Guid addressId)
    {
        // Перевіряємо належність
        var userAddress = await db.UserAddresses
            .Include(ua => ua.Address)
            .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AddressId == addressId)
            ?? throw new ApiException("ADDRESS_NOT_FOUND", "Address not found.", 404);

        // Тут можна додати поле IsDefault в UserAddress, якщо треба
        // Поки що просто повертаємо адресу

        return MapToDto(userAddress.Address);
    }

    #region Helpers

    private static void ValidateAddress(CreateAddressDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName))
            throw new ApiException("VALIDATION_ERROR", "FirstName is required.", 400);

        if (string.IsNullOrWhiteSpace(dto.LastName))
            throw new ApiException("VALIDATION_ERROR", "LastName is required.", 400);

        if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
            throw new ApiException("VALIDATION_ERROR", "PhoneNumber is required.", 400);

        if (string.IsNullOrWhiteSpace(dto.CityRef))
            throw new ApiException("VALIDATION_ERROR", "City is required.", 400);

        // Перевірка в залежності від типу доставки
        if (dto.DeliveryType == DeliveryType.Warehouse)
        {
            if (string.IsNullOrWhiteSpace(dto.WarehouseRef))
                throw new ApiException("VALIDATION_ERROR", "Warehouse is required for warehouse delivery.", 400);
        }
        else if (dto.DeliveryType == DeliveryType.Doors)
        {
            if (string.IsNullOrWhiteSpace(dto.Street) || string.IsNullOrWhiteSpace(dto.House))
                throw new ApiException("VALIDATION_ERROR", "Street and House are required for address delivery.", 400);
        }
    }

    public AddressResponseDto MapToDto(Address address)
    {
        // Формуємо адресу для відображення
        var formattedAddress = address.DeliveryType switch
        {
            DeliveryType.Warehouse =>
                $"{address.CityName}, {address.WarehouseDescription ?? $"Відділення №{address.WarehouseNumber}"}",

            DeliveryType.Doors =>
                $"{address.CityName}, {address.Street} {address.House}" +
                (string.IsNullOrWhiteSpace(address.Flat) ? "" : $", кв. {address.Flat}"),

            DeliveryType.Postomat =>
                $"{address.CityName}, {address.PostomatDescription}",

            _ => address.CityName ?? ""
        };

        return new AddressResponseDto
        {
            Id = address.Id,
            FirstName = address.FirstName,
            LastName = address.LastName,
            MiddleName = address.MiddleName,
            PhoneNumber = address.PhoneNumber,
            DeliveryType = address.DeliveryType,
            FormattedAddress = formattedAddress,
            CityName = address.CityName,
            WarehouseDescription = address.WarehouseDescription,
            Street = address.Street,
            House = address.House,
            Flat = address.Flat,
            AdditionalInfo = address.AdditionalInfo
        };
    }

    #endregion Helpers
}