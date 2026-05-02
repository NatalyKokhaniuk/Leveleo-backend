using LeveLEO.Data;
using LeveLEO.Features.Shipping.DTO;
using LeveLEO.Features.Shipping.Models;
using LeveLEO.Infrastructure.Delivery;
using Microsoft.EntityFrameworkCore;

namespace LeveLEO.Features.Shipping.Services;

public class AddressService(AppDbContext db, INovaPoshtaService novaPoshta) : IAddressService
{
    public async Task<AddressResponseDto> CreateAddressAsync(string userId, CreateAddressDto dto)
    {
        ValidateAddress(dto);

        var address = BuildAddressFromCreateDto(dto);
        await EnrichNpDescriptionsAsync(dto, address).ConfigureAwait(false);

        db.Addresses.Add(address);

        var ownedCount = await db.UserAddresses.CountAsync(ua => ua.UserId == userId);
        var makePrimary = dto.SetAsPrimary || ownedCount == 0;

        if (makePrimary)
            await ClearAllPrimaryAddressesAsync(userId);

        var userAddress = new UserAddress
        {
            UserId = userId,
            AddressId = address.Id,
            IsDefault = makePrimary
        };

        db.UserAddresses.Add(userAddress);
        await db.SaveChangesAsync();

        return MapToDto(address, userAddress.IsDefault);
    }

    public async Task<AddressResponseDto> UpdateAddressAsync(string userId, Guid addressId, UpdateAddressDto dto)
    {
        var userAddress = await db.UserAddresses
            .Include(ua => ua.Address)
            .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AddressId == addressId)
            ?? throw new ApiException(
                "ADDRESS_NOT_FOUND",
                "Address not found or does not belong to user.",
                404
            );

        var address = userAddress.Address;

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

        if (dto.WarehouseNumber.HasValue)
            address.WarehouseNumber = dto.WarehouseNumber.Value;

        if (dto.WarehouseDescription.HasValue)
            address.WarehouseDescription = dto.WarehouseDescription.Value;

        if (dto.PostomatRef.HasValue)
            address.PostomatRef = dto.PostomatRef.Value;

        if (dto.PostomatDescription.HasValue)
            address.PostomatDescription = dto.PostomatDescription.Value;

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

        NormalizePostomatRefFromWarehouseRef(address);
        await EnrichNpDescriptionsAfterUpdateAsync(address, dto).ConfigureAwait(false);

        address.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return MapToDto(address, userAddress.IsDefault);
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

        var wasDefault = userAddress.IsDefault;
        var uid = userId;

        db.UserAddresses.Remove(userAddress);

        var usedInOrders = await db.Orders.AnyAsync(o => o.AddressId == addressId);
        if (!usedInOrders)
            db.Addresses.Remove(userAddress.Address);

        await db.SaveChangesAsync();

        if (wasDefault)
            await PromoteLatestPrimaryAsync(uid);
    }

    public async Task<AddressResponseDto> GetAddressByIdAsync(string userId, Guid addressId)
    {
        var userAddress = await db.UserAddresses
            .Include(ua => ua.Address)
            .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AddressId == addressId)
            ?? throw new ApiException("ADDRESS_NOT_FOUND", "Address not found.", 404);

        return MapToDto(userAddress.Address, userAddress.IsDefault);
    }

    public async Task<List<AddressResponseDto>> GetUserAddressesAsync(string userId)
    {
        var rows = await db.UserAddresses
            .Include(ua => ua.Address)
            .Where(ua => ua.UserId == userId)
            .OrderByDescending(ua => ua.IsDefault)
            .ThenByDescending(ua => ua.Address.CreatedAt)
            .ToListAsync();

        return [.. rows.Select(ua => MapToDto(ua.Address, ua.IsDefault))];
    }

    public async Task<AddressResponseDto> SetDefaultAddressAsync(string userId, Guid addressId)
    {
        var userAddress = await db.UserAddresses
            .Include(ua => ua.Address)
            .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AddressId == addressId)
            ?? throw new ApiException("ADDRESS_NOT_FOUND", "Address not found.", 404);

        await ClearAllPrimaryAddressesAsync(userId);
        userAddress.IsDefault = true;
        await db.SaveChangesAsync();

        return MapToDto(userAddress.Address, true);
    }

    private async Task ClearAllPrimaryAddressesAsync(string userId) =>
        await db.UserAddresses
            .Where(ua => ua.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(ua => ua.IsDefault, false));

    private async Task PromoteLatestPrimaryAsync(string userId)
    {
        if (!await db.UserAddresses.AnyAsync(ua => ua.UserId == userId))
            return;

        await ClearAllPrimaryAddressesAsync(userId);

        var next = await db.UserAddresses
            .Include(ua => ua.Address)
            .Where(ua => ua.UserId == userId)
            .OrderByDescending(ua => ua.Address.CreatedAt)
            .FirstAsync();

        next.IsDefault = true;
        await db.SaveChangesAsync();
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
            throw new ApiException("VALIDATION_ERROR", "CityRef (settlementRef) is required.", 400);

        if (dto.DeliveryType == DeliveryType.Warehouse)
        {
            if (string.IsNullOrWhiteSpace(dto.WarehouseRef))
                throw new ApiException("VALIDATION_ERROR", "WarehouseRef is required for warehouse delivery.", 400);
        }
        else if (dto.DeliveryType == DeliveryType.Postomat)
        {
            if (string.IsNullOrWhiteSpace(dto.PostomatRef) &&
                string.IsNullOrWhiteSpace(dto.WarehouseRef))
                throw new ApiException("VALIDATION_ERROR", "Postomat requires warehouseRef or postomatRef (NP settlement point ref).", 400);
        }
        else if (dto.DeliveryType == DeliveryType.Doors)
        {
            if (string.IsNullOrWhiteSpace(dto.Street) || string.IsNullOrWhiteSpace(dto.House))
                throw new ApiException("VALIDATION_ERROR", "Street and House are required for address delivery.", 400);
        }
    }

    public AddressResponseDto MapToDto(Address address, bool isDefault = false)
    {
        var formattedAddress = address.GetSummaryDisplayLine();

        return new AddressResponseDto
        {
            Id = address.Id,
            IsDefault = isDefault,
            FirstName = address.FirstName,
            LastName = address.LastName,
            MiddleName = address.MiddleName,
            PhoneNumber = address.PhoneNumber,
            DeliveryType = address.DeliveryType,
            WarehouseRef = address.WarehouseRef,
            PostomatRef = address.PostomatRef,
            FormattedAddress = formattedAddress,
            CityName = address.CityName,
            WarehouseDescription = address.WarehouseDescription,
            PostomatDescription = address.PostomatDescription,
            Street = address.Street,
            House = address.House,
            Flat = address.Flat,
            AdditionalInfo = address.AdditionalInfo
        };
    }

    /// <summary>
    /// Після PATCH: опис відділення/поштомату з НП, якщо змінились місто/реф або опис порожній (як при створенні).
    /// </summary>
    private async Task EnrichNpDescriptionsAfterUpdateAsync(Address address, UpdateAddressDto dto)
    {
        var cityRef = address.CityRef?.Trim();
        if (string.IsNullOrWhiteSpace(cityRef))
        {
            return;
        }

        var warehouseTouched =
            dto.CityRef.HasValue
            || dto.WarehouseRef.HasValue
            || (dto.DeliveryType.HasValue && dto.DeliveryType.Value == DeliveryType.Warehouse);

        if (address.DeliveryType == DeliveryType.Warehouse
            && !string.IsNullOrWhiteSpace(address.WarehouseRef)
            && (warehouseTouched || string.IsNullOrWhiteSpace(address.WarehouseDescription)))
        {
            var wh = await novaPoshta.GetWarehouseByCityAndRefAsync(cityRef, address.WarehouseRef).ConfigureAwait(false);
            if (wh?.Description != null)
            {
                address.WarehouseDescription = wh.Description.Trim();
                if (string.IsNullOrWhiteSpace(address.WarehouseNumber) && !string.IsNullOrWhiteSpace(wh.Number))
                {
                    address.WarehouseNumber = wh.Number.Trim();
                }
            }
        }

        var postomatRef = address.PostomatRef ?? address.WarehouseRef;
        var postomatTouched =
            dto.CityRef.HasValue
            || dto.PostomatRef.HasValue
            || dto.WarehouseRef.HasValue
            || (dto.DeliveryType.HasValue && dto.DeliveryType.Value == DeliveryType.Postomat);

        if (address.DeliveryType == DeliveryType.Postomat
            && !string.IsNullOrWhiteSpace(postomatRef)
            && (postomatTouched || string.IsNullOrWhiteSpace(address.PostomatDescription)))
        {
            var pm = await novaPoshta.GetWarehouseByCityAndRefAsync(cityRef, postomatRef).ConfigureAwait(false);
            if (pm?.Description != null)
            {
                address.PostomatDescription = pm.Description.Trim();
            }
        }
    }

    /// <summary>Як у <see cref="BuildAddressFromCreateDto"/>: реф поштомата може прийти у <see cref="CreateAddressDto.WarehouseRef"/>.</summary>
    private static void NormalizePostomatRefFromWarehouseRef(Address address)
    {
        if (address.DeliveryType != DeliveryType.Postomat)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(address.PostomatRef)
            || string.IsNullOrWhiteSpace(address.WarehouseRef))
        {
            return;
        }

        address.PostomatRef = address.WarehouseRef.Trim();
        address.WarehouseRef = null;
    }

    private async Task EnrichNpDescriptionsAsync(CreateAddressDto dto, Address address)
    {
        var cityRef = dto.CityRef?.Trim();
        if (string.IsNullOrWhiteSpace(cityRef))
        {
            return;
        }

        if (dto.DeliveryType == DeliveryType.Warehouse
            && !string.IsNullOrWhiteSpace(address.WarehouseRef)
            && string.IsNullOrWhiteSpace(address.WarehouseDescription))
        {
            var wh = await novaPoshta.GetWarehouseByCityAndRefAsync(cityRef, address.WarehouseRef).ConfigureAwait(false);
            if (wh?.Description != null)
            {
                address.WarehouseDescription = wh.Description.Trim();
                if (string.IsNullOrWhiteSpace(address.WarehouseNumber) && !string.IsNullOrWhiteSpace(wh.Number))
                {
                    address.WarehouseNumber = wh.Number.Trim();
                }
            }
        }
        else if (dto.DeliveryType == DeliveryType.Postomat
            && !string.IsNullOrWhiteSpace(address.PostomatRef)
            && string.IsNullOrWhiteSpace(address.PostomatDescription))
        {
            var pm = await novaPoshta.GetWarehouseByCityAndRefAsync(cityRef, address.PostomatRef).ConfigureAwait(false);
            if (pm?.Description != null)
            {
                address.PostomatDescription = pm.Description.Trim();
            }
        }
    }

    private static Address BuildAddressFromCreateDto(CreateAddressDto dto)
    {
        var address = new Address
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(dto.MiddleName) ? string.Empty : dto.MiddleName.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            DeliveryType = dto.DeliveryType,
            CityRef = dto.CityRef?.Trim(),
            CityName = string.IsNullOrWhiteSpace(dto.CityName) ? null : dto.CityName.Trim(),
            AdditionalInfo = string.IsNullOrWhiteSpace(dto.AdditionalInfo) ? null : dto.AdditionalInfo.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        switch (dto.DeliveryType)
        {
            case DeliveryType.Warehouse:
                address.WarehouseRef = dto.WarehouseRef?.Trim();
                address.WarehouseNumber = string.IsNullOrWhiteSpace(dto.WarehouseNumber) ? null : dto.WarehouseNumber.Trim();
                address.WarehouseDescription = string.IsNullOrWhiteSpace(dto.WarehouseDescription)
                    ? null
                    : dto.WarehouseDescription.Trim();
                ClearStreetFields(address);
                ClearPostomatFields(address);
                break;

            case DeliveryType.Postomat:
                address.PostomatRef = !string.IsNullOrWhiteSpace(dto.PostomatRef)
                    ? dto.PostomatRef.Trim()
                    : dto.WarehouseRef?.Trim();

                address.PostomatDescription = string.IsNullOrWhiteSpace(dto.PostomatDescription)
                    ? (string.IsNullOrWhiteSpace(dto.WarehouseDescription) ? null : dto.WarehouseDescription.Trim())
                    : dto.PostomatDescription.Trim();

                ClearWarehouseNpFields(address);
                ClearStreetFields(address);
                break;

            case DeliveryType.Doors:
                address.StreetRef = dto.StreetRef?.Trim();
                address.Street = dto.Street?.Trim();
                address.House = dto.House?.Trim();
                address.Flat = string.IsNullOrWhiteSpace(dto.Flat) ? null : dto.Flat.Trim();
                address.Floor = string.IsNullOrWhiteSpace(dto.Floor) ? null : dto.Floor.Trim();
                ClearWarehouseNpFields(address);
                ClearPostomatFields(address);
                break;
        }

        return address;
    }

    private static void ClearWarehouseNpFields(Address address)
    {
        address.WarehouseRef = null;
        address.WarehouseNumber = null;
        address.WarehouseDescription = null;
    }

    private static void ClearPostomatFields(Address address)
    {
        address.PostomatRef = null;
        address.PostomatDescription = null;
    }

    private static void ClearStreetFields(Address address)
    {
        address.StreetRef = null;
        address.Street = null;
        address.House = null;
        address.Flat = null;
        address.Floor = null;
    }

    #endregion Helpers
}
